document.addEventListener("DOMContentLoaded", function () {
    let list = document.querySelectorAll(".navigation li");

    if (list.length > 1) {  
        list[1].classList.add("hovered");
    }
});

let list = document.querySelectorAll(".navigation li");

function activeLink() {
    list.forEach((item) => {
        item.classList.remove("hovered"); 
    });
    this.classList.add("hovered");
}

list.forEach((item, index) => {
    if (index > 0) { 
        item.addEventListener("mouseover", activeLink);
    }
});

let toggle = document.querySelector(".toggle");
let navigation = document.querySelector(".navigation");
let main = document.querySelector(".main");

toggle.onclick = function () {
    navigation.classList.toggle("active");
    main.classList.toggle("active");
};
