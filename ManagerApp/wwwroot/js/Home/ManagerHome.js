document.addEventListener("DOMContentLoaded", function () {
    let list = document.querySelectorAll(".navigation li a");
    let toggle = document.querySelector(".toggle");
    let navigation = document.querySelector(".navigation");
    let main = document.querySelector(".main");

    if (list.length === 0) return;

    let currentPath = window.location.pathname.replace(/\/$/, "").toLowerCase();
    let activeIndex = -1;

    list.forEach((item, index) => {
        let linkPath = item.getAttribute("href").replace(/\/$/, "").toLowerCase();
        if (linkPath === currentPath || (currentPath === "" && linkPath === "/")) {
            activeIndex = index;
        }
    });

    let savedIndex = -1;
    try {
        if (typeof Storage !== "undefined") {
            let storedIndex = Number(localStorage.getItem("activeMenuIndex"));
            if (Number.isInteger(storedIndex) && storedIndex >= 0 && storedIndex < list.length) {
                savedIndex = storedIndex;
            }
        }
    } catch (e) {
        console.warn("localStorage недоступен. Активный элемент не сохранится.");
    }

    if (activeIndex !== -1) {
        list[activeIndex]?.parentElement.classList.add("hovered");
        try {
            localStorage.setItem("activeMenuIndex", activeIndex);
        } catch (e) {
            console.warn("localStorage недоступен.");
        }
    } else if (savedIndex !== -1) {
        list[savedIndex]?.parentElement.classList.add("hovered");
    }

    function activeLink(event) {
        list.forEach((item) => item.parentElement.classList.remove("hovered"));
        this.parentElement.classList.add("hovered");

        let index = [...list].indexOf(this);
        if (index !== -1) {
            try {
                localStorage.setItem("activeMenuIndex", index);
            } catch (e) {
                console.warn("localStorage недоступен. Активный элемент не сохранится.");
            }
        }
    }

    list.forEach((item) => item.addEventListener("click", activeLink));

    let menuState = "open";
    try {
        if (typeof Storage !== "undefined") {
            menuState = localStorage.getItem("menuState") || "open";
        }
    } catch (e) {
        console.warn("localStorage недоступен.");
    }

    function applyMenuState() {
        let isLargeScreen = window.innerWidth > 991;

        if (isLargeScreen) {
            if (menuState === "open") {
                navigation.classList.add("active");
                main.classList.add("active");
            } else {
                navigation.classList.remove("active");
                main.classList.remove("active");
            }
        } else {
            if (!navigation.classList.contains("active") && menuState === "closed") {
                navigation.classList.remove("active");
                main.classList.remove("active");
            }
        }
    }

    applyMenuState();

    if (toggle) {
        toggle.addEventListener("click", () => {
            let isOpen = navigation.classList.toggle("active");
            main.classList.toggle("active", isOpen);
            menuState = isOpen ? "open" : "closed";
            try {
                localStorage.setItem("menuState", menuState);
            } catch (e) {
                console.warn("localStorage недоступен.");
            }
        });
    }

    window.addEventListener("resize", () => {
        applyMenuState();
    });
});
