/**
 * An object which displays a directory of media items in a way that allows
 * the user to navigate between directories and media items
 * @param {string} mediaType
 * @param {string} controller
 * @class
 */
function FileIndex(mediaType, controller) {

    this.$customFilter = $("[data-custom-filter]");

    this.$applyFilter = $("[data-apply-filter]");

    this.$addTag = $("[data-add-tag]");

    this.$tagName = $("[data-tag-name]");

    this.$selectableForTagging = $("[data-tag-selector]").not("[data-template]");

    this.$tagList = $("[data-tags-for-folder]");

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
}

FileIndex.prototype.getSubFilesAsync = function () {

    var deferred = $.Deferred();
    var payload = {
        currentDirectory: this.directoryPath,
        mediaType: this.mediaType,
        page: this.page,
        filter: this.filter
    };

    $.ajax({
        url: "../Media/GetSubFiles",
        method: "GET",
        data: payload,
        dataType: "json",
        contentType: "application/json",
        success: function (paginatedSubFiles) {


            deferred.resolve(paginatedSubFiles);
        },
        error: function (xhr) {

            alert("[" + xhr.status + "] " + xhr.statusText);
            deferred.reject();
        }
    });

    return deferred.promise();
}

FileIndex.prototype.renderPages = function () {

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

FileIndex.prototype.getSubDirectoriesAsync = function () {

    var deferred = $.Deferred();
    var payload = {
        currentDirectory: this.directoryPath,
        mediaType: this.mediaType
    };

    $.ajax({
        url: "../Media/GetSubDirectories",
        method: "GET",
        data: payload,
        dataType: "json",
        contentType: "application/json",
        success: function (directoryHierarchy) {

            deferred.resolve(directoryHierarchy);
        },
        error: function (xhr) {

            alert("[" + xhr.status + "] " + xhr.statusText);
            deferred.reject();
        }
    });

    return deferred.promise();
}

FileIndex.prototype.renderSubFilesAsync = function () {

    var deferred = $.Deferred();
    var self = this;

    this.getSubFilesAsync()
        .then(function (paginatedSubFiles) {

            self.totalFiles = paginatedSubFiles.Total;

            var $files = $("[data-files]");

            if (paginatedSubFiles.CurrentPage.length !== 0) {

                paginatedSubFiles.CurrentPage.forEach(function (subFileInfo, subFileIndex) {

                    self.renderSubFile(subFileInfo, ((self.page - 1) * self.filesPerPage) + subFileIndex + 1);
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

FileIndex.prototype.renderSubFile = function (subFileInfo, subFileIndex) {

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
        subFileIndex +
        "&filter=" +
        this.filter;

    $filePreview.find("a").attr("href", fileViewingPage);
    $filePreview.find("[data-file-name]").text(subFileInfo.Name);
    $filePreview.find("[data-tag-selector]").attr("data-path", subFileInfo.TaggingUrl);
    $filePreview.css("background-image", "url(\"" + this.cdnPath + "/" + subFileInfo.ThumbnailUrl + "\")");

    this.$fileList.append($filePreview);
}

FileIndex.prototype.renderSubDirectoriesAsync = function () {

    var deferred = $.Deferred();
    var self = this;

    this.getSubDirectoriesAsync()
        .then(function (subDirectoryInfos) {

            subDirectoryInfos.forEach(function (subDirectoryInfo) {

                self.renderSubDirectory(subDirectoryInfo);
            });

            self.$selectableForTagging = $("[data-tag-selector]").not("[data-template]");

            deferred.resolve();
        })
        .fail(function () {

            deferred.reject();
        });

    return deferred.promise();
};

FileIndex.prototype.renderSubDirectory = function (subDirectoryInfo) {

    var $directoryPreview = this.$directoryTemplate
        .clone()
        .show()
        .removeAttr("data-template");

    $directoryPreview.find("a").attr("href", window.location.origin + "/" + window.location.pathname + "?path=" + subDirectoryInfo.Path);
    $directoryPreview.find("[data-directory-name]").text(subDirectoryInfo.TaggingUrl);
    $directoryPreview.find("[data-tag-selector]").attr("data-path", subDirectoryInfo.Path);
    $directoryPreview.css("background-image", "url(\"" + this.cdnPath + "/" + subDirectoryInfo.ThumbnailUrl + "\")");

    this.$directoryList.append($directoryPreview);
}

/**
 * Gets the hierarchy for the current directory from the API
 * @returns {JQuery.Promise<any>}
 */
FileIndex.prototype.getDirectoryHierarchyAsync = function () {

    var deferred = $.Deferred();
    var payload = {
        currentDirectory: this.directoryPath,
        mediaType: this.mediaType
    };

    $.ajax({
        url: "../Media/GetDirectoryHierarchy",
        method: "GET",
        data: payload,
        dataType: "json",
        contentType: "application/json",
        success: function (directoryHierarchy) {

            deferred.resolve(directoryHierarchy);
            console.log(directoryHierarchy);
        },
        error: function (xhr) {

            alert("[" + xhr.status + "] " + xhr.statusText);
            deferred.reject();
        }
    });

    return deferred.promise();
}

FileIndex.prototype.renderSteppingStonesAsync = function () {

    var self = this;
    var deferred = $.Deferred();

    this.getDirectoryHierarchyAsync()
        .then(function (directoryHierarchy) {

            while (directoryHierarchy !== null) {

                var $steppingStone = FileIndex.generateSteppingStone(directoryHierarchy.Name, directoryHierarchy.Path);
                self.$steppingStones.prepend($steppingStone);

                directoryHierarchy = directoryHierarchy.Parent;
            }

            deferred.resolve();
        })
        .fail(function () {

            deferred.reject();
        });

    return deferred.promise();
}

FileIndex.generateSteppingStone = function (text, path) {

    var $steppingstone = $("<a>")
        .text(text)
        .attr("href", window.location.origin + window.location.pathname + "?path=" + path)
        .addClass("steppingstone_item");

    return $steppingstone;
}

/**
 * Initialises the file index
 * @param {string} mediaType
 */
FileIndex.prototype.initialiseAsync = function () {

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

            self.addTagging();

            deferred.resolve();
        })
        .fail(function () {

            deferred.reject();
        });

    return deferred.promise();
}

FileIndex.prototype.addEventHandlers = function () {

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

FileIndex.prototype.addTagging = function () {

    var self = this;
    var numberOfFiles = $("[data-files]").find("li").length;
    var numberOfDirectories = $("[data-directories]").find("li").length;

    if (numberOfFiles === 0 && numberOfDirectories === 0) {

        return;
    }

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

FileIndex.prototype.addTagsAsync = function () {

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

FileIndex.prototype.addTagsToDirectoriesAsync = function () {

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
    var payload = {
        directoryPaths: directoryPaths,
        tags: tagNames.split(","),
        mediaType: this.mediaType
    };

    $.ajax({
        url: "../Tag/AddTagsToDirectories",
        method: "POST",
        data: JSON.stringify(payload),
        dataType: "json",
        contentType: "application/json",
        success: function (isSuccess) {

            if (isSuccess) {

                payload.tags.forEach(function (tagName) {

                    self.addTagToList(tagName);
                });

                self.$tagName.val("");

                deferred.resolve();
            }
            else {

                alert("Failed to add tags to directories");
                deferred.reject();
            }
        },
        error: function (xhr) {

            alert("[" + xhr.status + "] " + xhr.statusText);
            deferred.reject();
        }
    });

    return deferred.promise();
}

FileIndex.prototype.addTagsToFilesAsync = function () {

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
    var payload = {
        filePaths: filePaths,
        tags: tagNames.split(","),
        mediaType: this.mediaType
    };

    $.ajax({
        url: "../Tag/AddTagsToFiles",
        method: "POST",
        data: JSON.stringify(payload),
        dataType: "json",
        contentType: "application/json",
        success: function (isSuccess) {

            if (isSuccess) {

                payload.tags.forEach(function (tagName) {

                    self.addTagToList(tagName);
                });

                deferred.resolve();
            }
            else {

                alert("Failed to add tags to directories");
                deferred.reject();
            }
        },
        error: function (xhr) {

            alert("[" + xhr.status + "] " + xhr.statusText);
            deferred.reject();
        }
    });

    return deferred.promise();
}

FileIndex.prototype.getTagsAsync = function () {

    var deferred = $.Deferred();

    $.ajax({
        url: "../Tag/GetDirectoryTags",
        method: "GET",
        data: {
            directoryName: this.directoryPath,
            mediaType: this.mediaType
        },
        dataType: "json",
        contentType: "application/json",
        success: function (tags) {

            deferred.resolve(tags);
        },
        error: function (xhr) {

            alert("[" + xhr.status + "] " + xhr.statusText);
            deferred.reject();
        }
    });

    return deferred.promise();
}

FileIndex.prototype.renderTagListAsync = function () {

    var deferred = $.Deferred();
    var self = this;

    this.getTagsAsync()
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

FileIndex.prototype.addTagToList = function (tagName) {

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

FileIndex.prototype.applyFilter = function () {

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