$(function () {

    var mediaView = new MediaView("image");

    // Load the image for the current page
    mediaView.navigatePagesAsync(0);

    // Initialise the rest of the page
    mediaView.slideshowThread();
    mediaView.addEventHandlers();
    mediaView.synchroniseFocus();
})