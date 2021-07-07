/**
 * An object which generates common UI elements
 * @class
 */
function MediaUiFactory() {

    /**
     * Generates stepping stones based on a directory hierarchy retrieved from the API
     * @param {any} directoryHierarchy
     */
    this.generateSteppingStones = function (directoryHierarchy, filter) {

        var $steppingStones = $("<div>");
        while (directoryHierarchy !== null) {

            var $steppingStone = this.generateSteppingStone(directoryHierarchy.Name, directoryHierarchy.Path, filter);
            $steppingStones.prepend($steppingStone);

            directoryHierarchy = directoryHierarchy.Parent;
        }

        return $steppingStones;
    }

    this.generateSteppingStone = function (text, path, filter) {

        var $steppingstone = $("<a>")
            .text(text)
            .attr("href", window.location.origin + window.location.pathname + "?path=" + path + "&filter=" + filter)
            .addClass("steppingstone_item");

        return $steppingstone;
    }
}