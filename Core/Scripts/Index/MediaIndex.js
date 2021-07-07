/**
 * An object which displays a directory of media items in a way that allows
 * the user to navigate between directories and media items
 * @param {string} mediaType
 * @param {string} controller
 * @class
 */
function MediaIndex(mediaType, controller) {

    this.$customFilter = $("[data-custom-filter]");

    this.$applyFilter = $("[data-apply-filter]");

    this.$selectableForTagging = $("[data-tag-selector]").not("[data-template]");

    this.$taggingContainer = $("[data-tagging]");

    this.$tagList = this.$taggingContainer.find("[data-tags-for-folder]");

    this.$tagName = this.$taggingContainer.find("[data-tag-name]");

    this.$addTag = this.$taggingContainer.find("[data-add-tag]");

    this.$clearFilter = $("[data-clear-filter]");

    this.$steppingStones = $("[data-stepping-stones]");

    this.$directoryTemplate = $("[data-template='directory']");

    this.$fileTemplate = $("[data-template='file']");

    this.$directoryList = $("[data-directory-list]");

    this.$fileList = $("[data-file-list]");

    this.$pages = $("[data-pages]");

    this.directoryPath = $("[name='Path']").val();

    this.cdnPath = $("[name='CdnPath']").val();

    this.page = +$("[name='Page']").val();

    this.filesPerPage = +$("[name='FilesPerPage']").val();

    this.filter = $("[name='Filter']").val();

    this.mediaType = mediaType;

    this.controller = controller;

    this.totalFiles = 0;

    this.mediaRepository = new MediaRepository();

    this.mediaUiFactory = new MediaUiFactory();

    this.tagRepository = new TagRepository();
}

MediaIndex.prototype.renderPages = function () {

    var totalPages = Math.ceil(this.totalFiles / this.filesPerPage);

    if (totalPages > 1) {

        var $pageList = this.$pages.find("[data-page-list]");
        var $totalPages = this.$pages.find("[data-total-pages]");

        $pageList.html("");
        $totalPages.text(totalPages + " Pages Total");

        for (var pageIndex = 1; pageIndex <= totalPages; pageIndex++) {

            var $currentPage = $("<a>").text(pageIndex);

            if (pageIndex != this.page) {

                var url = "../" + this.controller + "/Index?path=" + this.directoryPath + "&page=" + pageIndex + "&filter=" + this.filter;
                $currentPage.addClass("button_item").attr("href", url);
            }
            else {

                $currentPage.addClass("button_item-current");
            }

            $pageList.append($currentPage);
        }

        this.$pages.show();
    }
    else {

        this.$pages.hide();
    }
}

MediaIndex.prototype.renderSubFilesAsync = function () {

    var deferred = $.Deferred();
    var self = this;

    this.mediaRepository.getSubFilesAsync(this.directoryPath, this.mediaType, this.page, this.filter)
        .then(function (paginatedSubFiles) {

            self.totalFiles = paginatedSubFiles.Total;

            var $files = $("[data-files]");

            if (paginatedSubFiles.CurrentPage.length !== 0) {

                paginatedSubFiles.CurrentPage.forEach(function (subFileInfo, subMediaIndex) {

                    self.renderSubFile(subFileInfo, ((self.page - 1) * self.filesPerPage) + subMediaIndex + 1);
                });
            }
            else {

                $files.find("h1").remove();
            }

            $files.show();

            self.$selectableForTagging = $("[data-tag-selector]").not("[data-template]");

            self.renderPages();

            deferred.resolve();
        })
        .fail(function () {

            deferred.reject();
        });

    return deferred.promise();
};

MediaIndex.prototype.renderSubFile = function (subFileInfo, subMediaIndex) {

    var $filePreview = this.$fileTemplate
        .clone()
        .show()
        .removeAttr("data-template");

    var fileViewingPage = window.location.origin +
        "/" +
        this.controller +
        "/" +
        "View?path=" +
        subFileInfo.Path +
        "&page=" +
        subMediaIndex +
        "&filter=" +
        this.filter;

    $filePreview.find("a").attr("href", fileViewingPage);
    $filePreview.find("[data-file-name]").text(subFileInfo.Name);
    $filePreview.find("[data-tag-selector]").attr("data-path", subFileInfo.TaggingUrl);
    $filePreview.css("background-image", "url(\"" + this.cdnPath + "/" + subFileInfo.ThumbnailUrl + "\")");

    this.$fileList.append($filePreview);
}

MediaIndex.prototype.renderSubDirectoriesAsync = function () {

    var deferred = $.Deferred();
    var self = this;

    this.mediaRepository.getSubDirectoriesAsync(this.directoryPath, this.mediaType, this.filter)
        .then(function (subDirectoryInfos) {

            var $directories = $("[data-directories]");

            if (subDirectoryInfos.length !== 0) {

                subDirectoryInfos.forEach(function (subDirectoryInfo) {

                    self.renderSubDirectory(subDirectoryInfo);
                });
            }
            else {

                $directories.find("h1").remove();
            }

            $directories.show();

            self.$selectableForTagging = $("[data-tag-selector]").not("[data-template]");

            deferred.resolve();
        })
        .fail(function () {

            deferred.reject();
        });

    return deferred.promise();
};

MediaIndex.prototype.renderSubDirectory = function (subDirectoryInfo) {

    var $directoryPreview = this.$directoryTemplate
        .clone()
        .show()
        .removeAttr("data-template");

    $directoryPreview.find("a").attr("href", window.location.origin + window.location.pathname + "?path=" + subDirectoryInfo.Path + "&filter=" + this.filter);
    $directoryPreview.find("[data-directory-name]").text(subDirectoryInfo.Name);
    $directoryPreview.find("[data-tag-selector]").attr("data-path", subDirectoryInfo.Path);
    $directoryPreview.css("background-image", "url(\"" + this.cdnPath + "/" + subDirectoryInfo.ThumbnailUrl + "\")");

    this.$directoryList.append($directoryPreview);
}

MediaIndex.prototype.renderSteppingStonesAsync = function () {

    var self = this;
    var deferred = $.Deferred();

    this.mediaRepository.getDirectoryHierarchyAsync(this.directoryPath, this.mediaType)
        .then(function (directoryHierarchy) {

            var $steppingStones = self.mediaUiFactory.generateSteppingStones(directoryHierarchy, self.filter);
            self.$steppingStones.html($steppingStones);
            deferred.resolve();
        })
        .fail(function () {

            deferred.reject();
        });

    return deferred.promise();
}

/**
 * Initialises the file index
 * @param {string} mediaType
 */
MediaIndex.prototype.initialiseAsync = function () {

    var deferred = $.Deferred();
    var self = this;

    this.addEventHandlers();

    var renderAsynchronously = $.when(
        this.renderSteppingStonesAsync(),
        this.renderSubDirectoriesAsync(),
        this.renderSubFilesAsync(),
        this.renderTagListAsync()
    );

    renderAsynchronously
        .then(function () {

            var numberOfMediaItems = $("[data-files],[data-directories]").find("li").length;

            if (numberOfMediaItems === 0) {

                var $files = $("[data-files]");
                var $noFilesMessage = $("<h1>").text(this.filter !== "" ? "No files found with filter " + self.filter : "No files found");

                $files.html($noFilesMessage);
            }

            self.addMediaDependentActions();

            deferred.resolve();
        })
        .fail(function () {

            deferred.reject();
        });

    return deferred.promise();
}

MediaIndex.prototype.addEventHandlers = function () {

    var self = this;

    this.$applyFilter.on("click",
        function () {

            self.applyFilter();
        });

    this.$customFilter.on("keypress",
        function (event) {

            var pressedKey = event.keyCode || event.which;
            var enterKey = 13;

            if (pressedKey === enterKey) {

                self.applyFilter();
            }
        }
    );

    this.$addTag.on("click", function () {

        self.addTagsAsync()
            .then(function () {

                alert("Tags added");
            });
    });

    this.$tagName.on("keypress",
        function (event) {

            var pressedKey = event.keyCode || event.which;
            var enterKey = 13;

            if (pressedKey === enterKey) {

                self.addTagsAsync()
                    .then(function () {

                        alert("Tags added")
                    });
            }
        }
    );

    this.$clearFilter.on("click", function () {

        self.$customFilter.val("");
        self.applyFilter();
    });
}

/**
 * Adds any actions to the page which require the files and directories to already be loaded
 */
MediaIndex.prototype.addMediaDependentActions = function () {

    var self = this;
    var numberOfFiles = $("[data-files]").find("li").length;
    var numberOfDirectories = $("[data-directories]").find("li").length;

    if (numberOfFiles === 0 && numberOfDirectories === 0) {

        this.$taggingContainer.hide();

        return;
    }

    this.$taggingContainer.show();

    var $mediaSelection = $("[data-media-selection]");

    var $selectAll = $("<button>")
        .addClass("btn btn-primary")
        .text("Select All")
        .on("click", function () {

            self.$selectableForTagging.prop("checked", true);
        });

    var $selectNone = $("<button>")
        .addClass("btn btn-default")
        .text("Select None")
        .on("click", function () {

            self.$selectableForTagging.prop("checked", false);
        });

    $mediaSelection.append($selectAll).append($selectNone);

}

MediaIndex.prototype.addTagsAsync = function () {

    var deferred = $.Deferred();
    var self = this;

    var addTagsToAll = $.when(
        this.addTagsToFilesAsync(),
        this.addTagsToDirectoriesAsync()
    );

    addTagsToAll
        .then(function () {

            self.$tagName.val("");
            deferred.resolve();
        })
        .fail(function () {

            deferred.reject();
        })

    return deferred.promise();
}

MediaIndex.prototype.addTagsToDirectoriesAsync = function () {

    var self = this;
    var tagNames = this.$tagName.val();
    var $selectedCheckboxes = this.$selectableForTagging.filter("[data-tag-type='directory']:checked");

    if (tagNames === "" || $selectedCheckboxes.length === 0) {

        var deferred = $.Deferred();
        deferred.resolve();
        return deferred.promise();
    }

    var directoryPaths = [];
    $selectedCheckboxes.each(function (_, selectedCheckbox) {

        var $selectedCheckbox = $(selectedCheckbox);
        var filePath = $selectedCheckbox.attr("data-path");
        directoryPaths.push(filePath);
    });

    var deferred = $.Deferred();
    var tags = tagNames.split(",");
    this.tagRepository.addTagsToDirectories(directoryPaths, tags, this.mediaType)
        .then(function () {

            tags.forEach(function (tagName) {

                self.addTagToList(tagName);
            });

            self.$tagName.val("");

            deferred.resolve();
        })
        .fail(function () {

            deferred.reject();
        });

    return deferred.promise();
}

MediaIndex.prototype.addTagsToFilesAsync = function () {

    var self = this;
    var tagNames = this.$tagName.val();
    var $selectedCheckboxes = this.$selectableForTagging.filter("[data-tag-type='file']:checked");

    if (tagNames === "" || $selectedCheckboxes.length === 0) {

        var deferred = $.Deferred();
        deferred.resolve();
        return deferred.promise();
    }

    var filePaths = [];
    $selectedCheckboxes.each(function (_, selectedCheckbox) {

        var $selectedCheckbox = $(selectedCheckbox);
        var filePath = $selectedCheckbox.attr("data-path");
        filePaths.push(filePath);
    });

    var deferred = $.Deferred();
    var tags = tagNames.split(",");
    this.tagRepository.addTagsToFiles(filePaths, tags, this.mediaType)
        .then(function () {

            tags.forEach(function (tagName) {

                self.addTagToList(tagName);
            });

            deferred.resolve();
        })
        .fail(function () {

            deferred.reject();
        });

    return deferred.promise();
}

MediaIndex.prototype.renderTagListAsync = function () {

    var deferred = $.Deferred();
    var self = this;

    this.tagRepository.getTagsAsync(this.directoryPath, this.mediaType, this.filter)
        .then(function (tag) {

            tag.forEach(function (tag) {

                self.addTagToList(tag.TagName);
            });

            deferred.resolve();
        })
        .fail(function () {

            deferred.reject();
        });

    return deferred.promise();
}

MediaIndex.prototype.addTagToList = function (tagName) {

    var self = this;
    var $tagLink = $("<li>").addClass("tagList_item").text(tagName);

    $tagLink.on("click", function () {

        var filterValue = self.$customFilter.val();

        if (filterValue !== "") {

            filterValue = (filterValue + "," + $tagLink.text());
        }
        else {

            filterValue = $tagLink.text();
        }

        self.$customFilter.val(filterValue)
        self.applyFilter();
    });

    this.$tagList.append($tagLink);
}

MediaIndex.prototype.applyFilter = function () {

    var filterString = this.$customFilter.val();
    var queryString = window.location.search;

    // If there are no queries currently set, add one with our filter
    if (queryString == null || queryString.length === 0) {

        window.location.search = "?filter=" + filterString;
    }

    var queryParameters = queryString.toLowerCase().substring(1).split("&");
    var isFound = false;

    for (var parameterIndex = 0; parameterIndex < queryParameters.length && isFound === false; parameterIndex++) {

        // If the current query is the filter, it's the right one
        if (queryParameters[parameterIndex].indexOf("filter=") === 0) {

            isFound = true;

            // Replace the filter with our own filter
            queryParameters[parameterIndex] = "filter=" + filterString;
        }
    }

    if (isFound) {

        window.location.search = "?" + queryParameters.join("&");
    }
    else {

        window.location.search += "&filter=" + filterString;
    }
}