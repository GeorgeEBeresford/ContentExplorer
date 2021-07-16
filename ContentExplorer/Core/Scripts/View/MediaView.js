function MediaView(mediaType, controller) {

    this.siteBaseDirectory = $("[site-base-directory]").val();
    this.cdn = $("[data-cdn-path]").val();
    this.relativeDirectory = $("[data-relative-directory]").val();
    this.filter = $("[data-filter]").val();

    this.$previousButton = $("[data-button='previous']");
    this.$nextButton = $("[data-button='next']");
    this.$mediaView = $("[data-media-view]");
    this.$media = this.$mediaView.find("[data-media]");
    this.$mediaName = this.$mediaView.find("[data-media-name]");
    this.$mediaLink = this.$mediaView.find("[data-media-link]");
    this.$pageButtons = $("[data-page-button-wrapper]");
    this.$currentPage = $("[data-current-page]");
    this.$slideshowDelay = $("[data-slideshow-delay]");
    this.$isSlideshowEnabled = $("[data-slideshow-enabled]");
    this.$navbars = $(".navbar,.MediaView_navbutton,.MediaView_navbutton-right,.steppingstone_list,.button_list");
    this.$mediaLoader = $("[data-loader='media']");
    this.$escapeslideShow = $("[data-stop-slideshow]");
    this.$steppingStones = $("[data-stepping-stones]");
    this.$maxPages = $("[data-max-pages]");
    this.$documentTitle = $("title");

    // If we're viewing a page where putting the preview for the current item in the middle would cause issues,
    // put the preview at the start (position 1). Otherwise, put the item in the middle.
    this.startingPreview = this.$currentPage.text() - 7 < 1 ? 1 : this.$currentPage.text() - 7;
    this.isSlideshowEnabled = this.$isSlideshowEnabled.is(":checked");
    this.slideshowDelay = +this.$slideshowDelay.val();

    /**
     * The controller for the current media type
     * @type {string}
     */
    this.controller = controller;

    /**
     * A repository for retrieving information about the current file or directory
     * @type {MediaRepository}
     */
    this.mediaRepository = new MediaRepository();

    /**
     * The kind of media we're processing
     * @type {string}
     */
    this.mediaType = mediaType;

    /**
     * A factory which produces common media-related elements
     * @type {MediaUiFactory}
     */
    this.mediaUiFactory = new MediaUiFactory();
}

/**
 * Initialises the current object
 * @returns {JQuery.Promise<void>}
 */
MediaView.prototype.initialiseAsync = function () {

    var deferred = $.Deferred();
    var self = this;

    // Load the media for the current page
    var renderDirectoryDependencies = $.when(
        this.renderSteppingStonesAsync(),
        this.renderPageButtonsAsync()
    );

    renderDirectoryDependencies
        .then(function () {

            self.navigatePagesAsync(0)
                .then(function() {


                    // Initialise the rest of the page
                    self.slideshowThread();
                    self.addEventHandlers();
                    self.synchroniseFocus();
                });

            deferred.resolve();
        })
        .fail(function () {

            deferred.reject();
        });


    return deferred.promise();
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
}

MediaView.prototype.renderSteppingStonesAsync = function () {

    var deferred = $.Deferred();
    var self = this;

    this.mediaRepository.getDirectoryHierarchyAsync(this.relativeDirectory, this.mediaType)
        .then(function (directoryHierarchy) {

            var $steppingStones = self.mediaUiFactory.generateSteppingStones(self.controller, directoryHierarchy, self.filter);
            self.$steppingStones.html($steppingStones);

            deferred.resolve();
        })
        .fail(function () {

            deferred.reject();
        });

    return deferred.promise();
}

MediaView.prototype.synchroniseFocus = function () {

    if (this.isSlideshowEnabled) {

        this.focusMedia();
    }
    else {

        this.defocusMedia();
    }
}

MediaView.prototype.renderPageButtonsAsync = function () {

    var deferred = $.Deferred();
    var self = this;

    var fileNumber = +this.$currentPage.text();
    var pageFromFileNumber = Math.ceil(fileNumber / 15);
    console.log("pageFromFileNumber: " + pageFromFileNumber);

    this.mediaRepository.getSubFilesAsync(this.relativeDirectory, this.mediaType, this.filter, this.startingPreview - 1, 15)
        .then(function (mediaPreviews) {

            self.$pageButtons.find("[data-page-button-list]").html("");
            self.$maxPages.val(mediaPreviews.Total).show();

            if (mediaPreviews.Total > 1) {

                self.renderPageButtons(mediaPreviews, pageFromFileNumber);
                self.$pageButtons.show();
            }

            deferred.resolve();
        })
        .fail(function() {

            deferred.reject();
        });

    return deferred.promise();
}

/**
 * 
 * @param {any} mediaViewModels - A collection of media view models that have been retrieved from the API
 * @param {number} page - The page from the index that this media item was retrieved from
 */
MediaView.prototype.renderPageButtons = function (mediaViewModels, page) {

    var numberOfPages = mediaViewModels.CurrentPage.length;
    for (var pageButtonIndex = 0; pageButtonIndex < numberOfPages; pageButtonIndex++) {

        var mediaViewModel = mediaViewModels.CurrentPage[pageButtonIndex];
        var pageNumber = this.startingPreview + pageButtonIndex;
        console.log("pageNumber: " + pageNumber);
        var $pageButton = $("<a>").attr("data-page-button", pageNumber);

        if (pageNumber !== +this.$currentPage.text()) {

            $pageButton
                .attr("href",
                    "/" +
                    this.controller +
                    "/" +
                    "View" +
                    "?path=" +
                    this.relativeDirectory +
                    "&page=" +
                    pageNumber +
                    "&filter=" +
                    this.filter)
                .addClass("button_medialink");
        }
        else {

            $pageButton.addClass("button_medialink-disabled");
        }

        var $pagePreview = $("<div>")
            .addClass("button_mediapreview")
            .css("background-image", "url(\"" + this.cdn + "/" + this.formatThumbnailUrl(mediaViewModel.ThumbnailUrl) + "\")")
            .css("background-size", "cover");

        $pageButton.append($pagePreview);

        this.$pageButtons.find("[data-page-button-list]").append($pageButton);
    }
}

/**
 * Ensures any thumbnail urls passed to page buttons have the correct formatting
 * @param {string} thumbnailUrl
 */
MediaView.prototype.formatThumbnailUrl = function (thumbnailUrl) {

    return thumbnailUrl;
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
    var maxPages = +this.$maxPages.val();

    if (incrementedPage < 1) {

        incrementedPage = maxPages;

    } else if (incrementedPage > maxPages) {

        incrementedPage = 1;
    }

    this.$currentPage.text(incrementedPage);
}

MediaView.prototype.refreshMediaDisplay = function (subFilePreview, previousMediaInformation) {

    this.$documentTitle.text(subFilePreview.Name + " | " + this.controller + " | ContentExplorer");
    this.$mediaName.text(subFilePreview.Name);

    this.$pageButtons.find("[data-page-button='" + this.$currentPage.text() + "']")
        .removeClass("button_medialink")
        .addClass("button_medialink-disabled")
        .removeAttr("href");

    this.$mediaLoader.hide();
    this.$media.show();

    this.$media.css("width", "");
    this.$media.css("height", "");
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
            .removeClass("mediaview_media-focused")
            .addClass("mediaview_media");

        this.$navbars.show();
    }
}

MediaView.prototype.focusMedia = function () {

    if (!this.$media.is(".MediaView_media-focused")) {

        this.$media
            .removeClass("mediaview_media")
            .addClass("mediaview_media-focused");

        this.$navbars.hide();
    }
}