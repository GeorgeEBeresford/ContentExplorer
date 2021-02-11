function VideoView() {

    this.siteBaseDirectory = $("[site-base-directory]").val();
    this.cdn = $("[data-cdn-path]").val();
    this.relativeDirectory = $("[data-relative-directory]").val();
    this.filter = $("[data-filter]").val();
    this.maxPages = +$("[data-max-pages]").val();

    this.$previousButton = $("[data-button='previous']");
    this.$nextButton = $("[data-button='next']");
    this.$videoView = $("[data-video-view]");
    this.$video = this.$videoView.find("[data-video]");
    this.$videoName = this.$videoView.find("[data-video-name]");
    this.$videoLink = this.$videoView.find("[data-video-link]");
    this.$pageButtons = $("[data-page-button]");
    this.$currentPage = $("[data-current-page]");
    this.$slideshowDelay = $("[data-slideshow-delay]");
    this.$isSlideshowEnabled = $("[data-slideshow-enabled]");
    this.$navbars = $(".navbar,.videoview_navbutton,.videoview_navbutton-right,.steppingstone_list,.button_list");
    this.$videoLoader = $("[data-loader='video']");
    this.$escapeslideShow = $("[data-stop-slideshow]");

    this.isSlideshowEnabled = this.$isSlideshowEnabled.is(":checked");
    this.slideshowDelay = +this.$slideshowDelay.val();
}

VideoView.prototype.addEventHandlers = function () {

    var self = this;

    this.$nextButton.on("click",
        function () {

            self.navigatePagesAsync(1);
        }
    );

    this.$previousButton.on("click",
        function () {

            self.navigatePagesAsync(-1);
        }
    );

    this.$slideshowDelay.on("input",
        function () {

            var parsedDelay = parseInt($(this).val());

            // Don't lag the browser
            if (!isNaN(parsedDelay) && parsedDelay >= 1) {

                self.slideshowDelay = parsedDelay;
            }
        }
    );

    this.$escapeslideShow.on("click",
        function() {

            self.$isSlideshowEnabled.click();
        }
    );

    this.$isSlideshowEnabled.on("click",
        function () {

            self.isSlideshowEnabled = $(this).is(":checked");
            self.synchroniseFocus();

            if (!self.isSlideshowEnabled) {

                self.unsetFocusedVideoDimensions();
            }
            else {

                self.setFocusedVideoDimensions();
            }
        }
    );


    this.$video.on("load",
        function () {

            self.$videoLoader.hide();
            self.$video.show();

            self.$video.css("width", "");
            self.$video.css("height", "");

            if (!self.isSlideshowEnabled) {

                self.unsetFocusedVideoDimensions();
            }
            else {

                self.setFocusedVideoDimensions();
            }
        }
    );
}

VideoView.prototype.synchroniseFocus = function () {

    if (this.isSlideshowEnabled) {

        this.focusVideo();
    }
    else {

        this.defocusVideo();
    }
}

VideoView.prototype.slideshowThread = function () {

    var self = this;

    setTimeout(
        function () {

            if (self.isSlideshowEnabled) {

                self.navigatePagesAsync(1);
            }

            self.slideshowThread();
        },
        self.slideshowDelay * 1000
    );
}

VideoView.prototype.getVideoPathAsync = function () {

    var deferred = $.Deferred();

    $.ajax({
        url: "../Video/GetVideoPath",
        method: "GET",
        data: {
            directoryPath: this.relativeDirectory,
            page: this.$currentPage.text(),
            filter: this.filter
        },
        dataType: "json",
        contentType: "application/json",
        success: function (videoViewModel) {

            deferred.resolve(videoViewModel);
        },
        error: function (xhr) {

            alert("[" + xhr.status + "] " + xhr.statusText);
            deferred.reject();
        }
    });

    return deferred.promise();
}

VideoView.prototype.navigatePagesAsync = function (pageIncrement) {

    var deferred = $.Deferred();
    var self = this;

    var previousVideoInformation = {

        videoName: this.$videoName.text(),
        pageId: this.$currentPage.text()
    };

    this.addPageIncrement(pageIncrement);

    this.getVideoPathAsync()
        .then(function (videoViewModel) {

            self.refreshVideoDisplay(videoViewModel, previousVideoInformation);
            deferred.resolve();
        })
        .fail(function () {

            deferred.reject();
        });

    return deferred.promise();
}

VideoView.prototype.addPageIncrement = function (pageIncrement) {

    var currentPage = +this.$currentPage.text();
    var incrementedPage = currentPage + pageIncrement;

    if (incrementedPage < 1) {

        incrementedPage = this.maxPages;

    } else if (incrementedPage > this.maxPages) {

        incrementedPage = 1;
    }

    this.$currentPage.text(incrementedPage);
}

VideoView.prototype.refreshVideoDisplay = function (videoViewModel, previousVideoInformation) {

    this.$video.attr("src", videoViewModel.VideoPath);
    this.$videoName.text(videoViewModel.VideoName);
    this.$videoLink.attr("href", videoViewModel.VideoPath);

    // Make current page navigatable
    this.$pageButtons.filter(".button_videolink-disabled")
        .removeClass("button_videolink-disabled")
        .addClass("button_videolink")
        .attr("href", "/Video/View?path=" + this.relativeDirectory + "&page=" + previousVideoInformation.pageId + "&filter=" + this.filter);

    this.$pageButtons.filter("[data-page-button='" + this.$currentPage.text() + "']")
        .removeClass("button_videolink")
        .addClass("button_videolink-disabled")
        .removeAttr("href");
}

VideoView.prototype.setFocusedVideoDimensions = function () {

    var originalWidth = this.$video.outerWidth();
    var originalHeight = this.$video.outerHeight();
    var aspectRatio = originalWidth / originalHeight;

    this.$video.css("width", "100%");
    var width = this.$video.outerWidth();

    this.$video.css("height", (width / aspectRatio) + "px");
    var height = this.$video.outerHeight();

    // If the video is taller than the window
    if (height > window.innerHeight) {

        this.$video.css("height", "100%");
        height = this.$video.outerHeight();

        this.$video.css("width", (height * aspectRatio) + "px");
    }
    else if (width > window.innerWidth) {

        this.$video.css("width", "100%");
        width = this.$video.outerWidth();

        this.$video.css("height", (width / aspectRatio) + "px");
    }

    var windowWidth = window.innerWidth;
    var videoAndWindowDifference = windowWidth - this.$video.outerWidth();

    this.$video.css("left", videoAndWindowDifference / 2 + "px");
}

VideoView.prototype.unsetFocusedVideoDimensions = function () {

    this.$video.css("width", "");
    this.$video.css("height", "");
    this.$video.css("left", "");
}

VideoView.prototype.defocusVideo = function () {

    if (!this.$video.is(".videoview_video")) {

        this.$video
            .removeClass("videoview_video-focused")
            .addClass("videoview_video");

        this.$navbars.show();
    }
}

VideoView.prototype.focusVideo = function () {

    if (!this.$video.is(".videoview_video-focused")) {

        this.$video
            .removeClass("videoview_video")
            .addClass("videoview_video-focused");

        this.$navbars.hide();
    }
}

$(function () {

    var videoView = new VideoView();

    // Load the video for the current page
    videoView.navigatePagesAsync(0);

    // Initialise the rest of the page
    videoView.slideshowThread();
    videoView.addEventHandlers();
    videoView.synchroniseFocus();
})