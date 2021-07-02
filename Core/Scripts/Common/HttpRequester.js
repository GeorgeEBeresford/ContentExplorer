/**
 * A wrapper object which sends ajax HTTP requests to the server in a more abstract way
 * @class
 */
function HttpRequester() {

    /**
     * Sends a GET request to the server and returns the results asynchronously
     * @param {string} action
     * @param {string} controller
     * @param {Object<string, any>} data
     * @returns {JQuery.Promise<any>}
     */
    this.getAsync = function (action, controller, data) {

        return this.sendRequestAsync("../" + controller + "/" + action, "GET", data)
    }

    /**
     * Sends a POST request to the server and returns the results asynchronously
     * @param {string} action
     * @param {string} controller
     * @param {Object<string, any>} data
     * @returns {JQuery.Promise<any>}
     */
    this.postAsync = function (action, controller, data) {

        return this.sendRequestAsync("../" + controller + "/" + action, "POST", data)
    }

    /**
     * Sends a request to the server and returns the results asynchronously
     * @param {string} action
     * @param {string} controller
     * @param {Object<string, any>} data
     * @returns {JQuery.Promise<any>}
     */
    this.sendRequestAsync = function (url, method, data) {

        var deferred = $.Deferred();

        $.ajax({

            url: url,
            method: method,
            dataType: "json",
            contentType: "application/json",
            data: method.toLowerCase() === "get" ? data : JSON.stringify(data),
            success: function (result) {

                deferred.resolve(result);
            },
            error: function (xhr) {

                deferred.reject(xhr);
            }
        });

        return deferred.promise();
    }
}