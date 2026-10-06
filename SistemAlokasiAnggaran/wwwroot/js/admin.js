// Sidebar toggle (100% jQuery & Aturan Request Compliance)
function toggleSidebar() {
    var $sidebar = $("#sidebar");
    var $mainContent = $("#mainContent");
    var $overlay = $("#sidebarOverlay");
    var $topbar = $(".topbar");
    var isMobile = window.innerWidth <= 768;

    var isClosed = $sidebar.hasClass("closed");
    if (isMobile) {
        if (isClosed) {
            $sidebar.removeClass("closed");
            $overlay.addClass("show");
        } else {
            $sidebar.addClass("closed");
            $overlay.removeClass("show");
        }
    } else {
        if (isClosed) {
            $sidebar.removeClass("closed");
            $mainContent.removeClass("sidebar-closed");
            $topbar.css("left", "250px");
        } else {
            $sidebar.addClass("closed");
            $mainContent.addClass("sidebar-closed");
            $topbar.css("left", "0");
        }
    }
}

function closeSidebarOverlay() {
    var $sidebar = $("#sidebar");
    var $overlay = $("#sidebarOverlay");
    $sidebar.addClass("closed");
    $overlay.removeClass("show");
}

$(function () {
    var isMobile = window.innerWidth <= 768;
    if (isMobile) {
        $("#sidebar").addClass("closed");
        $("#sidebarOverlay").removeClass("show");
    }

    $(window).on("resize", function () {
        var mobileNow = window.innerWidth <= 768;
        if (!mobileNow) {
            $("#sidebar").removeClass("closed");
            $("#mainContent").removeClass("sidebar-closed");
            $(".topbar").css("left", "250px");
            $("#sidebarOverlay").removeClass("show");
        } else {
            $("#sidebar").addClass("closed");
            $("#mainContent").addClass("sidebar-closed");
            $(".topbar").css("left", "0");
            $("#sidebarOverlay").removeClass("show");
        }
    });
});
