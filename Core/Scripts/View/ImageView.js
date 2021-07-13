/// <reference path="./MediaView.js"/>

function ImageView() {

    MediaView.call(this, "image", "Image");
}

ImageView.prototype = Object.create(MediaView.prototype);

ImageView.prototype.refreshMediaDisplay = function (subFilePreview, previousMediaInformation) {

    // Make current page navigatable
    this.$pageButtons.find(".button_medialink-disabled")
        .removeClass("button_medialink-disabled")
        .addClass("button_medialink")
        .attr("href", "/Image/View?path=" + this.relativeDirectory + "&page=" + previousMediaInformation.pageId + "&filter=" + this.filter);

    MediaView.prototype.refreshMediaDisplay.call(this, subFilePreview, previousMediaInformation);

    this.$media.attr("src", subFilePreview.ContentUrl).attr("alt", subFilePreview.Name);
    this.$mediaLink.attr("href", subFilePreview.ContentUrl);

    if (!this.isSlideshowEnabled) {

        this.unsetFocusedMediaDimensions();
    }
    else {

        this.setFocusedMediaDimensions();
    }
}

$(function () {

    var mediaView = new ImageView();
    mediaView.initialiseAsync();
})