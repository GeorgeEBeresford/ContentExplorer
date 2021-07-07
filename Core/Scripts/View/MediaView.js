function MediaView(mediaType) {

    this.siteBaseDirectory = $("[site-base-directory]").val();
    this.cdn = $("[data-cdn-path]").val();
    this.relativeDirectory = $("[data-relative-directory]").val();
    this.filter = $("[data-filter]").val();
    this.maxPages = +$("[data-max-pages]").val();

    this.$previousButton = $("[data-button='previous']");
    this.$nextButton = $("[data-button='next']");
    this.$MediaView = $("[data-media-view]");
    this.$media = this.$MediaView.find("[data-media]");
    this.$mediaName = this.$MediaView.find("[data-media-name]");
    this.$mediaLink = this.$MediaView.find("[data-media-link]");
    this.$pageButtons = $("[data-page-button]");
    this.$currentPage = $("[data-current-page]");
    this.$slideshowDelay = $("[data-slideshow-delay]");
    this.$isSlideshowEnabled = $("[data-slideshow-enabled]");
    this.$navbars = $(".navbar,.MediaView_navbutton,.MediaView_navbutton-right,.steppingstone_list,.button_list");
    this.$mediaLoader = $("[data-loader='media']");
    this.$escapeslideShow = $("[data-stop-slideshow]");

    this.isSlideshowEnabled = this.$isSlideshowEnabled.is(":checked");
    this.slideshowDelay = +this.$slideshowDelay.val();
    this.mediaRepository = new MediaRepository();
    this.mediaType = mediaType;
}

MediaView.prototype.addEventHandlers = function () {

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
        function () {

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

MediaView.prototype.synchroniseFocus = function () {

    if (this.isSlideshowEnabled) {

        this.focusMedia();
    }
    else {

        this.defocusMedia();
    }
}

MediaView.prototype.slideshowThread = function () {

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

MediaView.prototype.navigatePagesAsync = function (pageIncrement) {

    var deferred = $.Deferred();
    var self = this;

    var previousMediaInformation = {

        mediaName: this.$mediaName.text(),
        pageId: this.$currentPage.text()
    };

    this.addPageIncrement(pageIncrement);

    this.mediaRepository.getSubFileAsync(this.relativeDirectory, +this.$currentPage.text(), this.mediaType, this.filter)
        .then(function (subFile) {

            self.refreshMediaDisplay(subFile, previousMediaInformation);
            deferred.resolve();
        })
        .fail(function () {

            deferred.reject();
        });

    return deferred.promise();
}

MediaView.prototype.addPageIncrement = function (pageIncrement) {

    var currentPage = +this.$currentPage.text();
    var incrementedPage = currentPage + pageIncrement;

    if (incrementedPage < 1) {

        incrementedPage = this.maxPages;

    } else if (incrementedPage > this.maxPages) {

        incrementedPage = 1;
    }

    this.$currentPage.text(incrementedPage);
}

MediaView.prototype.refreshMediaDisplay = function (subFilePreview, previousMediaInformation) {

    this.$media.attr("src", subFilePreview.ContentUrl).attr("alt", subFilePreview.Name);
    this.$mediaName.text(subFilePreview.Name);
    this.$mediaLink.attr("href", subFilePreview.Content);

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

MediaView.prototype.setFocusedMediaDimensions = function () {

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

MediaView.prototype.unsetFocusedMediaDimensions = function () {

    this.$media.css("width", "");
    this.$media.css("height", "");
    this.$media.css("left", "");
}

MediaView.prototype.defocusMedia = function () {

    if (!this.$media.is(".MediaView_media")) {

        this.$media
            .removeClass("MediaView_media-focused")
            .addClass("MediaView_media");

        this.$navbars.show();
    }
}

MediaView.prototype.focusMedia = function () {

    if (!this.$media.is(".MediaView_media-focused")) {

        this.$media
            .removeClass("MediaView_media")
            .addClass("MediaView_media-focused");

        this.$navbars.hide();
    }
}