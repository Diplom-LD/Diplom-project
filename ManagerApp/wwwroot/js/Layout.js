document.addEventListener("DOMContentLoaded", function () {
    const navigation = document.querySelector(".navigation");
    const main = document.querySelector(".main");
    const toggle = document.querySelector(".toggle");
    const menuLinks = document.querySelectorAll(".navigation li a");

    if (!navigation || !main || menuLinks.length === 0) {
        console.warn("⚠️ Navigation elements not found. Skipping script.");
        return;
    }

    let currentPath = window.location.pathname.replace(/\/$/, "").toLowerCase();

    let savedIndex = localStorage.getItem("activeMenuIndex");
    let activeIndex = -1;

    menuLinks.forEach((item, index) => {
        let linkPath = item.getAttribute("href");
        if (!linkPath || linkPath === "#") return;

        linkPath = linkPath.replace(/\/$/, "").toLowerCase();
        if (linkPath === currentPath || (currentPath === "" && linkPath === "/")) {
            activeIndex = index;
        }
    });

    if (activeIndex !== -1) {
        menuLinks.forEach(el => el.parentElement.classList.remove("hovered"));
        menuLinks[activeIndex]?.parentElement.classList.add("hovered");
        localStorage.setItem("activeMenuIndex", activeIndex);
    } else if (savedIndex !== null && menuLinks[savedIndex]) {
        menuLinks[savedIndex].parentElement.classList.add("hovered");
    }

    menuLinks.forEach((item, index) => {
        item.addEventListener("click", function () {
            menuLinks.forEach(el => el.parentElement.classList.remove("hovered"));
            this.parentElement.classList.add("hovered");
            localStorage.setItem("activeMenuIndex", index);
        });
    });

    let menuState = localStorage.getItem("menuState") || "open";

    function applyMenuState() {
        let isLargeScreen = window.innerWidth > 991;
        navigation.classList.toggle("active", menuState === "open");
        main.classList.toggle("active", menuState === "open");
    }
    applyMenuState();

    if (toggle) {
        toggle.addEventListener("click", () => {
            let isOpen = navigation.classList.toggle("active");
            main.classList.toggle("active", isOpen);
            localStorage.setItem("menuState", isOpen ? "open" : "closed");
        });
    }

    window.addEventListener("resize", applyMenuState);
});