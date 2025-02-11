const container = document.getElementById('main-container');
const registerBtn = document.getElementById('register');
const loginBtn = document.getElementById('login');

// Добавление событий для переключения между формами
registerBtn.addEventListener('click', () => {
    container.classList.add("active");
});

loginBtn.addEventListener('click', () => {
    container.classList.remove("active");
});
