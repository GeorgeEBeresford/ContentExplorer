/**
 * An object which sends and retrieves tag-related information to and from the server
 * @class
 */
function TagRepository() {

    /**
     * An object which deals with sending HTTP requests to the server
     * @type {HttpRequester}
     */
    var httpRequester = new HttpRequester();

    var controller = "Tag";

    /**
     * Adds the specified tags to the provided directory paths
     * @param {Array<string>} directoryPaths
     * @param {Array<string>} tags
     * @param {string} mediaType
     * @returns {JQuery.Promise<Array<any>>}
     */
    this.addTagsToDirectories = function (directoryPaths, tags, mediaType) {

        var deferred = $.Deferred();
        var payload = {
            directoryPaths: directoryPaths,
            tags: tags,
            mediaType: mediaType
        };

        httpRequester.postAsync("AddTagsToDirectories", controller, payload)
            .then(function (isSuccess) {

                if (isSuccess) {

                    deferred.resolve();
                }
                else {

                    alert("Failed to add tags to directories");
                    deferred.reject();
                }
            })
            .fail(function (xhr) {

                alert("[" + xhr.status + "] " + xhr.statusText);
                deferred.reject();
            });

        return deferred.promise();
    }

    /**
     * Adds the specified tags to the provided directory paths
     * @param {Array<string>} filePaths
     * @param {Array<string>} tags
     * @param {string} mediaType
     * @returns {JQuery.Promise<Array<any>>}
     */
    this.addTagsToFiles = function (filePaths, tags, mediaType) {

        var deferred = $.Deferred();
        var payload = {
            filePaths: filePaths,
            tags: tags,
            mediaType: mediaType
        };

        httpRequester.postAsync("AddTagsToFiles", controller, payload)
            .then(function (isSuccess) {

                if (isSuccess) {

                    deferred.resolve();
                }
                else {

                    alert("Failed to add tags to files");
                    deferred.reject();
                }
            })
            .fail(function (xhr) {

                alert("[" + xhr.status + "] " + xhr.statusText);
                deferred.reject();
            });

        return deferred.promise();
    }

    /**
     * Retrieves the tags for the given directory and mediaType
     * @param {string} directoryPath -
     * @param {string} mediaType - 
     * @param {string} filter - 
     * @returns {JQuery.Promise<any>}
     */
    this.getTagsAsync = function (directoryPath, mediaType, filter) {

        var deferred = $.Deferred();
        var payload = {
            directoryName: directoryPath,
            mediaType: mediaType,
            filter: filter
        };

        httpRequester.getAsync("GetDirectoryTags", controller, payload)
            .then(function (tags) {

                deferred.resolve(tags);
            })
            .fail(function (xhr) {

                alert("[" + xhr.status + "] " + xhr.statusText);
                deferred.reject();
            });

        return deferred.promise();
    }

    /**
     * Retrieves the tags for the given file and mediaType
     * @param {string} filePath -
     * @param {string} mediaType -
     * @returns {JQuery.Promise<any>}
     */
    this.getFileTagsAsync = function(filePath, mediaType) {

        var deferred = $.Deferred();
        var payload = {
            fileName: filePath,
            mediaType: mediaType
        };

        httpRequester.getAsync("GetFileTags", controller, payload)
            .then(function (tags) {

                deferred.resolve(tags);
            })
            .fail(function (xhr) {

                alert("[" + xhr.status + "] " + xhr.statusText);
                deferred.reject();
            });

        return deferred.promise();
    }
}