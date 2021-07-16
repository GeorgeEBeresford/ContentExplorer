function ImageIndex() {

    MediaIndex.call(this, "image", "Image");

    this.$actions = $("[data-actions='image']");
}

ImageIndex.prototype = Object.create(MediaIndex.prototype);

ImageIndex.prototype.moveSelectedDirectoriesAsync = function () {

    var self = this;
    var deferred = $.Deferred();
    var $selectedCheckboxes = this.$selectableForTagging.filter("[data-tag-type='directory']:checked");

    if ($selectedCheckboxes.length === 0) {

        deferred.resolve();
        return deferred.promise();
    }

    var newDirectoryPath = prompt("New Directory Path: ", this.directoryPath);
    if (newDirectoryPath === "") {

        deferred.resolve();
        return deferred.promise();
    }

    var directoryPaths = [];
    $selectedCheckboxes.each(function (_, selectedCheckbox) {

        var $selectedCheckbox = $(selectedCheckbox);
        var directoryPath = $selectedCheckbox.attr("data-path");
        directoryPaths.push(directoryPath);
    });

    this.mediaRepository.moveSubDirectories(directoryPaths, newDirectoryPath, this.mediaType)
        .then(function () {

            self.$directoryList.html("");
            self.$fileList.html("");

            // We've just moved some files around. Recalculate which files we need to show
            self.initialiseAsync()
                .then(function () {

                    deferred.resolve();
                })
                .fail(function () {

                    deferred.reject();
                });
        })
        .fail(function () {

            deferred.reject();
        });

    return deferred.promise();
}

ImageIndex.prototype.moveSelectedFilesAsync = function () {

    var self = this;
    var deferred = $.Deferred();
    var $selectedCheckboxes = this.$selectableForTagging.filter("[data-tag-type='file']:checked");

    if ($selectedCheckboxes.length === 0) {

        deferred.resolve();
        return deferred.promise();
    }

    var newDirectoryPath = prompt("New Directory Path: ", this.directoryPath);
    if (newDirectoryPath === "") {

        deferred.resolve();
        return deferred.promise();
    }

    var filePaths = [];
    $selectedCheckboxes.each(function (_, selectedCheckbox) {

        var $selectedCheckbox = $(selectedCheckbox);
        var filePath = $selectedCheckbox.attr("data-path");
        filePaths.push(filePath);
    });

    this.mediaRepository.moveSubFiles(filePaths, newDirectoryPath, this.mediaType)
        .then(function () {

            self.$directoryList.html("");
            self.$fileList.html("");

            // We've just moved some files around. Recalculate which files we need to show
            self.initialiseAsync()
                .then(function () {

                    deferred.resolve();
                })
                .fail(function () {

                    deferred.reject();
                });
        })
        .fail(function () {

            deferred.reject();
        });

    return deferred.promise();
}

ImageIndex.prototype.addMediaDependentActions = function () {

    MediaIndex.prototype.addMediaDependentActions.call(this);

    var self = this;

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

    var $moveFiles = $("<button>")
        .addClass("btn btn-default")
        .text("Move selected")
        .on("click", function () {

            self.moveSelectedFilesAsync();
            self.moveSelectedDirectoriesAsync();
        });

    this.$actions
        .html("")
        .append($rebuildThumbnailsButton)
        .append($reformatNames)
        .append($moveFiles);
}

$(function () {

    var mediaIndex = new ImageIndex();
    mediaIndex.initialiseAsync();
})