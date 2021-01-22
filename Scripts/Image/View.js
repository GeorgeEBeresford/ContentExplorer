function ImageView() {

    this.siteBaseDirectory = $("[site-base-directory]").val();
    this.cdn = $("[data-cdn-path]").val();
    this.relativeDirectory = $("[data-relative-directory]").val();
    this.filter = $("[data-filter]").val();
    this.maxPages = +$("[data-max-pages]").val();

    this.$previousButton = $("[data-button='previous']");
    this.$nextButton = $("[data-button='next']");
    this.$imageView = $("[data-image-view]");
    this.$image = this.$imageView.find("[data-image]");
    this.$imageName = this.$imageView.find("[data-image-name]");
    this.$imageLink = this.$imageView.find("[data-image-link]");
    this.$pageButtons = $("[data-page-button]");
    this.$currentPage = $("[data-current-page]");
    this.$slideshowDelay = $("[data-slideshow-delay]");
    this.$isSlideshowEnabled = $("[data-slideshow-enabled]");

    this.isSlideshowEnabled = this.$isSlideshowEnabled.is(":checked");
    this.slideshowDelay = +this.$slideshowDelay.val();
}

ImageView.prototype.addEventHandlers = function () {

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

    this.$isSlideshowEnabled.on("click",
        function () {

            self.isSlideshowEnabled = $(this).is(":checked");
            self.synchroniseFocus();
        }
    );
}

ImageView.prototype.slideshowThread = function () {

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

ImageView.prototype.synchroniseFocus = function() {

    if(this.isSlideshowEnabled) {

        this.focusImage();
    }
    else {

        this.defocusImage();
    }
}

ImageView.prototype.getImagePathAsync = function () {

    var deferred = $.Deferred();

    $.ajax({
        url: "../Image/GetImagePath",
        method: "GET",
        data: {
            directoryPath: this.relativeDirectory,
            page: this.$currentPage.text(),
            filter: this.filter
        },
        dataType: "json",
        contentType: "application/json",
        success: function (imageViewModel) {

            deferred.resolve(imageViewModel);
        },
        error: function (xhr) {

            alert("[" + xhr.status + "] " + xhr.statusText);
            deferred.reject();
        }
    });

    return deferred.promise();
}

ImageView.prototype.navigatePagesAsync = function (pageIncrement) {

    var deferred = $.Deferred();
    var self = this;

    var previousImageInformation = {

        imageName: this.$imageName.text(),
        pageId: this.$currentPage.text()
    };

    this.addPageIncrement(pageIncrement);

    this.getImagePathAsync()
        .then(function (imageViewModel) {

            self.refreshImageDisplay(imageViewModel, previousImageInformation);
            deferred.resolve();
        })
        .fail(function () {

            deferred.reject();
        });

    return deferred.promise();
}

ImageView.prototype.addPageIncrement = function (pageIncrement) {

    var currentPage = +this.$currentPage.text();
    var incrementedPage = currentPage + pageIncrement;

    if (incrementedPage < 1) {

        incrementedPage = this.maxPages;

    } else if (incrementedPage > this.maxPages) {

        incrementedPage = 1;
    }

    this.$currentPage.text(incrementedPage);
}

ImageView.prototype.refreshImageDisplay = function (imageViewModel, previousImageInformation) {

    this.$image.attr("src", imageViewModel.ImagePath);
    this.$imageName.text(imageViewModel.ImageName);
    this.$imageLink.attr("href", imageViewModel.ImagePath);

    // Make current page navigatable
    this.$pageButtons.filter(".button_imagelink-disabled")
        .removeClass("button_imagelink-disabled")
        .addClass("button_imagelink")
        .attr("href", "/Image/View?path=" + this.relativeDirectory + "&page=" + previousImageInformation.pageId + "&filter=" + this.filter);

    this.$pageButtons.filter("[data-page-button='" + this.$currentPage.text() + "']")
        .removeClass("button_imagelink")
        .addClass("button_imagelink-disabled")
        .removeAttr("href");

    this.synchroniseFocus();
}

ImageView.prototype.defocusImage = function () {

    this.$image
        .removeClass("imageview_image-focused")
        .addClass("imageview_image");
}

ImageView.prototype.focusImage = function () {

    this.$image
        .removeClass("imageview_image")
        .addClass("imageview_image-focused");

    var width = this.$image.innerWidth();
    var height = this.$image.innerHeight();
    var aspectRatio = width / height;

    if (aspectRatio >= 1) {

        this.$image.css("width", "100%");
        this.$image.css("height", (height * aspectRatio) + "px");
    } else {

        this.$image.css("height", "100%");
        this.$image.css("width", (width / aspectRatio) + "px");
    }

    height = this.$image.innerHeight();
    if (height > window.innerHeight) {

        this.$image.css("height", "100%");

        height = this.$image.innerHeight();

        this.$image.css("width", (height * aspectRatio) + "px");
    } else {

        this.$image.css("width", "100%");

        width = this.$image.innerWidth();

        this.$image.css("height", (width * aspectRatio) + "px");
    }

    var windowWidth = window.innerWidth;
    var imageAndWindowDifference = windowWidth - this.$image.innerWidth;

    this.$image.css("left", imageAndWindowDifference / 2 + "px");
}

$(function () {

    var imageView = new ImageView();
    imageView.addEventHandlers();
    imageView.synchroniseFocus();
    imageView.slideshowThread();
})