/// <reference path="./MediaView.js"/>

function VideoView() {

    MediaView.call(this, "video", "Video");

    this.$videoContainer = $("[data-video-container]");

    this.$video = null;

    this.$mediaSource = null;

    this.extensionsToMimeTypes = {
        "mp4": "video/mp4",
        "ogg": "video/ogg"
    };
}

VideoView.prototype = Object.create(MediaView.prototype);

VideoView.prototype.refreshMediaDisplay = function (subFilePreview, previousMediaInformation) {

    // Make current page navigatable
    this.$pageButtons.find(".button_medialink-disabled")
        .removeClass("button_medialink-disabled")
        .addClass("button_medialink")
        .attr("href", "/Video/View?path=" + this.relativeDirectory + "&page=" + previousMediaInformation.pageId + "&filter=" + this.filter);

    MediaView.prototype.refreshMediaDisplay.call(this, subFilePreview, previousMediaInformation);

    if (this.$video === null) {

        this.$video = $("<video>")
            .attr({
                "controls": "controls",
                "loop": "loop",
                "data-video": "data-video"
            })
            .addClass("mediaView_media");
    }

    if (this.$mediaSource == null) {

        this.$mediaSource = $("<source>");
    }

    this.$mediaSource.attr("src", subFilePreview.ContentUrl);

    var contentUrlParts = subFilePreview.ContentUrl.split(".");
    var extension = contentUrlParts[contentUrlParts.length - 1];
    var detectedMimeType = this.extensionsToMimeTypes[extension];
    if (typeof (detectedMimeType) !== "undefined") {

        this.$mediaSource.attr("type", detectedMimeType);
    }

    if (this.$videoContainer.find("[data-video]").length === 0) {

        this.$videoContainer.append(this.$video);
    }

    if (this.$video.find("[data-media-source]").length === 0) {

        this.$video.append(this.$mediaSource);
    }
}

/**
 * Ensures any thumbnail urls passed to page buttons have the correct formatting
 * @param {string} thumbnailUrl
 */
VideoView.prototype.formatThumbnailUrl = function (thumbnailUrl) {

    return thumbnailUrl;
}

$(function () {

    var mediaView = new VideoView();
    mediaView.initialiseAsync();
})