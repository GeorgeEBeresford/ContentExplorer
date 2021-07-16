/**
 * An object that displays a list of videos in a directory
 * @class
 */
function VideoIndex() {

    MediaIndex.call(this, "video", "Video");

    this.$actions = $("[data-actions='video']");
}

VideoIndex.prototype = Object.create(MediaIndex.prototype);

VideoIndex.prototype.addMediaDependentActions = function () {

    MediaIndex.prototype.addMediaDependentActions.call(this);

    var $rebuildThumbnailsButton = $("<a>")
        .attr("href", "../" + this.controller + "/" + "RebuildThumbnails?path=" + this.directoryPath)
        .attr("target", "_blank")
        .append(
            $("<div>").addClass("btn btn-default").text("Rebuild Thumbnails")
        );

    var $reformatNames = $("<a>")
        .attr("href", "../" + this.controller + "/" + "ReformatNames?path=" + this.directoryPath)
        .attr("target", "_blank")
        .append(
            $("<div>").addClass("btn btn-default").text("Reformat Names")
        );

    var $convertUnplayableVideos = $("<a>")
        .attr("href", "../" + this.controller + "/" + "ConvertUnplayableVideos?path=" + this.directoryPath)
        .attr("target", "_blank")
        .append(
            $("<div>").addClass("btn btn-default").text("Convert Unplayable Videos")
        );

    this.$actions
        .append($rebuildThumbnailsButton)
        .append($reformatNames)
        .append($convertUnplayableVideos);
}

$(function () {

    var mediaIndex = new VideoIndex();
    mediaIndex.initialiseAsync();
})