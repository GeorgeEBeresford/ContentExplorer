function VideoView() {

    this.siteBaseDirectory = $("[site-base-directory]").val();
    this.cdn = $("[data-cdn-path]").val();
    this.relativeDirectory = $("[data-relative-directory]").val();
    this.filter = $("[data-filter]").val();
    this.maxPages = +$("[data-max-pages]").val();

    this.$previousButton = $("[data-button='previous']");
    this.$nextButton = $("[data-button='next']");
    this.$mediaView = $("[data-media-view]");
    this.$media = this.$mediaView.find("[data-media]");
    this.$mediaName = this.$mediaView.find("[data-media-name]");
    this.$mediaLink = this.$mediaView.find("[data-media-link]");
    this.$pageButtons = $("[data-page-button]");
    this.$currentPage = $("[data-current-page]");
    this.$slideshowDelay = $("[data-slideshow-delay]");
    this.$isSlideshowEnabled = $("[data-slideshow-enabled]");
    this.$navbars = $(".navbar,.mediaview_navbutton,.mediaview_navbutton-right,.steppingstone_list,.button_list");
    this.$mediaLoader = $("[data-loader='media']");
    this.$escapeslideShow = $("[data-stop-slideshow]");

    this.isSlideshowEnabled = this.$isSlideshowEnabled.is(":checked");
    this.slideshowDelay = +this.$slideshowDelay.val();
    this.mediaRepository = new MediaRepository();
    this.mediaType = "video";
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

                self.unsetFocusedMediaDimensions();
            }
            else {

                self.setFocusedMediaDimensions();
            }
        }
    );


    this.$media.on("load",
        function () {

            self.$mediaLoader.hide();
            self.$media.show();

            self.$media.css("width", "");
            self.$media.css("height", "");

            if (!self.isSlideshowEnabled) {

                self.unsetFocusedMediaDimensions();
            }
            else {

                self.setFocusedMediaDimensions();
            }
        }
    );
}

VideoView.prototype.synchroniseFocus = function () {

    if (this.isSlideshowEnabled) {

        this.focusMedia();
    }
    else {

        this.defocusMedia();
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

VideoView.prototype.navigatePagesAsync = function (pageIncrement) {

    var deferred = $.Deferred();
    var self = this;

    var previousMediaInformation = {

        mediaName: this.$mediaName.text(),
        pageId: this.$currentPage.text()
    };

    this.addPageIncrement(pageIncrement);

    this.mediaRepository.getSubFileAsync(this.relativeDirectory, this.$currentPage.text(), this.mediaType, this.filter)
        .then(function (mediaPreview) {

            self.refreshMediaDisplay(mediaPreview, previousMediaInformation);
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

VideoView.prototype.refreshMediaDisplay = function (mediaViewModel, previousMediaInformation) {

    this.$media.attr("src", mediaViewModel.ContentPath);
    this.$mediaName.text(mediaViewModel.Name);
    this.$mediaLink.attr("href", mediaViewModel.ContentPath);

    // Make current page navigatable
    this.$pageButtons.filter(".button_medialink-disabled")
        .removeClass("button_medialink-disabled")
        .addClass("button_medialink")
        .attr("href", "/Media/View?path=" + this.relativeDirectory + "&page=" + previousMediaInformation.pageId + "&filter=" + this.filter);

    this.$pageButtons.filter("[data-page-button='" + this.$currentPage.text() + "']")
        .removeClass("button_medialink")
        .addClass("button_medialink-disabled")
        .removeAttr("href");
}

VideoView.prototype.setFocusedMediaDimensions = function () {

    var originalWidth = this.$media.outerWidth();
    var originalHeight = this.$media.outerHeight();
    var aspectRatio = originalWidth / originalHeight;

    this.$media.css("width", "100%");
    var width = this.$media.outerWidth();

    this.$media.css("height", (width / aspectRatio) + "px");
    var height = this.$media.outerHeight();

    // If the media is taller than the window
    if (height > window.innerHeight) {

        this.$media.css("height", "100%");
        height = this.$media.outerHeight();

        this.$media.css("width", (height * aspectRatio) + "px");
    }
    else if (width > window.innerWidth) {

        this.$media.css("width", "100%");
        width = this.$media.outerWidth();

        this.$media.css("height", (width / aspectRatio) + "px");
    }

    var windowWidth = window.innerWidth;
    var mediaAndWindowDifference = windowWidth - this.$media.outerWidth();

    this.$media.css("left", mediaAndWindowDifference / 2 + "px");
}

VideoView.prototype.unsetFocusedMediaDimensions = function () {

    this.$media.css("width", "");
    this.$media.css("height", "");
    this.$media.css("left", "");
}

VideoView.prototype.defocusMedia = function () {

    if (!this.$media.is(".mediaview_media")) {

        this.$media
            .removeClass("mediaview_media-focused")
            .addClass("mediaview_media");

        this.$navbars.show();
    }
}

VideoView.prototype.focusMedia = function () {

    if (!this.$media.is(".mediaview_media-focused")) {

        this.$media
            .removeClass("mediaview_media")
            .addClass("mediaview_media-focused");

        this.$navbars.hide();
    }
}

$(function () {

    var mediaView = new VideoView();

    // Load the media for the current page
    mediaView.navigatePagesAsync(0);

    // Initialise the rest of the page
    mediaView.slideshowThread();
    mediaView.addEventHandlers();
    mediaView.synchroniseFocus();
})