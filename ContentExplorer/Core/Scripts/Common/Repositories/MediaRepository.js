/**
 * An object which sends and retrieves media-related information to and from the server
 * @class
 */
function MediaRepository() {

    /**
     * An object which deals with sending HTTP requests to the server
     * @type {HttpRequester}
     */
    var httpRequester = new HttpRequester();

    var controller = "Media";

    /**
     * Retrieves the subdirectories of any given directory for the specified mediaType
     * @param {string} currentDirectory
     * @param {string} mediaType
     * @returns {JQuery.Promise<Array<any>>}
     */
    this.getSubDirectoriesAsync = function (currentDirectory, mediaType, filter) {

        var deferred = $.Deferred();
        var payload = {
            currentDirectory: currentDirectory,
            mediaType: mediaType,
            filter: filter
        };

        httpRequester.getAsync("GetSubDirectories", controller, payload)
            .then(function (directoryHierarchy) {

                deferred.resolve(directoryHierarchy);
            })
            .fail(function (xhr) {

                alert("[" + xhr.status + "] " + xhr.statusText);
                deferred.reject();
            });

        return deferred.promise();
    }

    /**
     * Retrieves the files directly inside any directory for the specified mediaType
     * @param {string} currentDirectory
     * @param {string} mediaType
     * @param {string} filter - A comma delimited list of filters
     * @param {number} skip - How many items to skip before we take a number of them
     * @param {number} take - The maximum number of items to return
     * @returns {JQuery.Promise<any>}
     */
    this.getSubFilesAsync = function (currentDirectory, mediaType, filter, skip, take) {

        var deferred = $.Deferred();
        var payload = {
            currentDirectory: currentDirectory,
            mediaType: mediaType,
            filter: filter,
            skip: skip,
            take: take
        };

        httpRequester.getAsync("GetSubFiles", controller, payload)
            .then(function (paginatedSubFiles) {

                deferred.resolve(paginatedSubFiles);
            })
            .fail(function (xhr) {

                alert("[" + xhr.status + "] " + xhr.statusText);
                deferred.reject();
            });

        return deferred.promise();
    }

    /**
     * Retrieves the subdirectories of any given directory for the specified mediaType
     * @param {string} currentDirectory
     * @param {string} mediaType
     * @returns {JQuery.Promise<any>}
     */
    this.getDirectoryHierarchyAsync = function (currentDirectory, mediaType) {

        var deferred = $.Deferred();
        var payload = {
            currentDirectory: currentDirectory,
            mediaType: mediaType
        };

        httpRequester.getAsync("GetDirectoryHierarchy", controller, payload)
            .then(function (directoryHierarchy) {

                deferred.resolve(directoryHierarchy);
            })
            .fail(function (xhr) {

                alert("[" + xhr.status + "] " + xhr.statusText);
                deferred.reject();
            });

        return deferred.promise();
    }

    /**
     * Retrieves information about a single file in a directory
     * @param {string} currentDirectory -
     * @param {number} page - A 1-based indexed page number
     * @param {string} mediaType
     * @param {string} filter
     * @returns {JQuery.Promise<any>}
     */
    this.getSubFileAsync = function (currentDirectory, page, mediaType, filter) {

        var deferred = $.Deferred();
        var payload = {
            currentDirectory: currentDirectory,
            mediaType: mediaType,
            page: page,
            filter: filter
        };

        httpRequester.getAsync("GetSubFile", controller, payload)
            .then(function (paginatedSubFiles) {

                deferred.resolve(paginatedSubFiles);
            })
            .fail(function (xhr) {

                alert("[" + xhr.status + "] " + xhr.statusText);
                deferred.reject();
            });

        return deferred.promise();
    }


    /**
     * Moves the specified subfiles to a new directory
     * @param {Array<string>} filePaths - 
     * @param {string} newDirectoryPath - 
     * @param {string} mediaType - 
     * @returns {JQuery.Promise<void>}
     */
    this.moveSubFiles = function (filePaths, newDirectoryPath, mediaType) {

        var deferred = $.Deferred();
        var payload = {
            filePaths: filePaths,
            newDirectoryPath: newDirectoryPath,
            mediaType: mediaType
        };

        httpRequester.postAsync("MoveSubFiles", controller, payload)
            .then(function (isSuccess) {

                if (isSuccess === true) {

                    alert("Files moved");
                    deferred.resolve();
                }
                else {

                    alert("Failed to move files");
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
     * Moves the specified subfiles to a new directory
     * @param {Array<string>} directoryPaths -
     * @param {string} newDirectoryPath -
     * @param {string} mediaType -
     * @returns {JQuery.Promise<void>}
     */
    this.moveSubDirectories = function (directoryPaths, newDirectoryPath, mediaType) {

        var deferred = $.Deferred();
        var payload = {
            directoryPaths: directoryPaths,
            newDirectoryPath: newDirectoryPath,
            mediaType: mediaType
        };

        httpRequester.postAsync("MoveSubDirectories", controller, payload)
            .then(function (isSuccess) {

                if (isSuccess === true) {

                    alert("Directories moved");
                    deferred.resolve();
                }
                else {

                    alert("Failed to move directories");
                    deferred.reject();
                }
            })
            .fail(function (xhr) {

                alert("[" + xhr.status + "] " + xhr.statusText);
                deferred.reject();
            });

        return deferred.promise();
    }
}