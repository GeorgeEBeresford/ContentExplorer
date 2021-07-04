/**
 * An object that displays a list of videos in a directory
 * @class
 */
function VideoIndex() {

    MediaIndex.call(this, "video", "Video");

    this.$actions = $("[data-actions='video']");
}

VideoIndex.prototype = Object.create(MediaIndex.prototype);

VideoIndex.prototype.renderSubDirectory = function (subDirectoryInfo) {

    var $directoryPreview = this.$directoryTemplate
        .clone().show().removeAttr("data-template");

    $directoryPreview.find("a").attr("href", window.location.origin + window.location.pathname + "?path=" + subDirectoryInfo.Path + "&filter=" + this.filter);
    $directoryPreview.find("[data-directory-name]").text(subDirectoryInfo.Name);
    $directoryPreview.find("[data-tag-selector]").attr("data-path", subDirectoryInfo.TaggingUrl);
    $directoryPreview.css("background-image", "url(\"" + this.cdnPath + "/" + subDirectoryInfo.ThumbnailUrl + ".jpg" + "\")");

    this.$directoryList.append($directoryPreview);
}

VideoIndex.prototype.renderSubFile = function (subFileInfo, subFileIndex) {

    var $filePreview = this.$fileTemplate
        .clone().show().removeAttr("data-template");

    var fileViewingPage =
        "/" +
        this.controller +
        "/" +
        "View?path=" +
        subFileInfo.Path +
        "&page=" +
        subFileIndex +
        "&filter=" +
        this.filter;

    $filePreview.find("a").attr("href", fileViewingPage);
    $filePreview.find("[data-file-name]").text(subFileInfo.Name);
    $filePreview.find("[data-tag-selector]").attr("data-path", subFileInfo.TaggingUrl);
    $filePreview.css("background-image", "url(\"" + this.cdnPath + "/" + subFileInfo.ThumbnailUrl + ".jpg" + "\")");

    this.$fileList.append($filePreview);
}

VideoIndex.prototype.addMediaDependentActions = function () {

    FileIndex.prototype.addMediaDependentActions.call(this);

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

    var fileIndex = new VideoIndex();
    fileIndex.initialiseAsync();
})