// add hovered class to selected list item
let list = document.querySelectorAll(".navigation li");

function activeLink() {
  list.forEach((item) => {
    item.classList.remove("hovered");
  });
  this.classList.add("hovered");
}

list.forEach((item) => item.addEventListener("mouseover", activeLink));

// Menu Toggle
let toggle = document.querySelector(".toggle");
let navigation = document.querySelector(".navigation");
let main = document.querySelector(".main");

toggle.onclick = function () {
  navigation.classList.toggle("active");
  main.classList.toggle("active");
};

// BTUCalculatorElements Additional params
function toggleParams() {
  const params = document.getElementById('additionalParams');
  params.style.display = params.style.display === 'none' || params.style.display === '' ? 'block' : 'none';
}

function toggleVentilation() {
  const hasVentilation = document.getElementById('hasVentilation').value;
  const airExchangeRateGroup = document.getElementById('airExchangeRateGroup');
  if (hasVentilation === 'true') {
      airExchangeRateGroup.style.display = 'block';
  } else {
      airExchangeRateGroup.style.display = 'none';
  }
}

function toggleWindowArea() {
  const hasLargeWindow = document.getElementById('hasLargeWindow').value;
  const windowAreaGroup = document.getElementById('windowAreaGroup');
  if (hasLargeWindow === 'true') {
      windowAreaGroup.style.display = 'block';
  } else {
      windowAreaGroup.style.display = 'none';
  }
}