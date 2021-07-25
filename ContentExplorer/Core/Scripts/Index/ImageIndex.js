function ImageIndex() {

    MediaIndex.call(this, "image", "Image");

    this.$actions = $("[data-actions='image']");
}

ImageIndex.prototype = Object.create(MediaIndex.prototype);

ImageIndex.prototype.addMediaDependentActions = function () {

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

    this.$actions
        .append($rebuildThumbnailsButton)
        .append($reformatNames);
}

$(function () {

    var mediaIndex = new ImageIndex();
    mediaIndex.initialiseAsync();
})