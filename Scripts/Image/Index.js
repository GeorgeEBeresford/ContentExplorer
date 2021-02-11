function ImageIndex() {

    this.$customFilter = $("[data-custom-filter]");

    this.$applyFilter = $("[data-apply-filter]");
}

ImageIndex.prototype.addEventHandlers = function () {

    var self = this;

    this.$applyFilter.on("click",
        function () {

            self.applyFilter();
        });
}

ImageIndex.prototype.applyFilter = function () {

    var filterString = this.$customFilter.val();
    var queryString = window.location.search;

    // If there are no queries currently set, add one with our filter
    if (queryString == null || queryString.length === 0) {

        window.location.search = "?filter=" + filterString;
        return;
    }

    var queryParameters = queryString.split("&");
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

        window.location.search = queryParameters.join("&");
    }
    else {

        window.location.search += "&filter=" + filterString;
    }
}

$(function() {

    var imageIndex = new ImageIndex();
    imageIndex.addEventHandlers();
})