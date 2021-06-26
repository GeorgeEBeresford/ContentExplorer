function FileIndex() {

    this.$customFilter = $("[data-custom-filter]");

    this.$applyFilter = $("[data-apply-filter]");

    this.$addTag = $("[data-add-tag]");

    this.$tagName = $("[data-tag-name]");

    this.$selectableForTagging = $("[data-tag-selector]")

    this.$tagList = $("[data-tags-for-folder]");

    this.$clearFilter = $("[data-clear-filter]");

    this.directoryName = $("[data-directory-name]").val();

    this.$selectAll = $("[data-select-all]");

    this.$selectNone = $("[data-select-none]")
}

FileIndex.prototype.initialiseAsync = function () {

    var deferred = $.Deferred();

    this.addEventHandlers();

    this.renderTagListAsync()
        .then(function () {

            deferred.resolve();
        })
        .fail(function () {

            deferred.reject();
        })

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

                alert("Tags added")
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

    this.$selectAll.on("click", function () {

        self.$selectableForTagging.prop("checked", true);
    });

    this.$selectNone.on("click", function () {

        self.$selectableForTagging.prop("checked", false);
    });
}

FileIndex.prototype.addTagsAsync = function () {

    return $.when(
        this.addTagsToFilesAsync(),
        this.addTagsToDirectoriesAsync()
    ).promise();
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
        tags: tagNames.split(",")
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
        tags: tagNames.split(",")
    };

    $.ajax({
        url: "../Tag/AddTagsToFiles",
        method: "POST",
        data: JSON.stringify(payload),
        dataType: "json",
        contentType: "application/json",
        success: function (fileViewModel) {

            payload.tags.forEach(function (tagName) {

                self.addTagToList(tagName);
            });

            self.$tagName.val("");

            deferred.resolve(fileViewModel);
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
            directoryName: this.directoryName
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