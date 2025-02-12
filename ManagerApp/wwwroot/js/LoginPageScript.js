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

const AUTH_SERVICE_URL = "http://localhost:5000";

function isValidLogin(login) {
    return /^[a-zA-Z0-9_-]{4,20}$/.test(login);
}

function isValidEmail(email) {
    return /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/.test(email);
}

function isValidPassword(password) {
    return password.length >= 6;
}

function isValidPhone(phone) {
    return /^\+\d{10,15}$/.test(phone);
}

function isValidName(name) {
    return /^[a-zA-Zа-яА-ЯёЁ]{1,50}$/.test(name);
}

function isValidAddress(address) {
    return address.length >= 6 && address.length <= 200;
}

// Валидация аутентификации (входа)
function validateSignIn(data) {
    let errors = [];

    if (!isValidEmail(data.Identifier) && !isValidLogin(data.Identifier)) {
        errors.push("Enter a valid email (e.g., user@example.com) or a username (4-20 characters, only letters, numbers, hyphens, and underscores).");
    }

    if (!isValidPassword(data.Password)) {
        errors.push("Password must be at least 6 characters.");
    }

    return errors;
}

// Валидация регистрации
function validateSignUp(data) {
    let errors = [];

    if (!isValidLogin(data.Login)) {
        errors.push("Login must be 4-20 characters and contain only letters, numbers, hyphens, and underscores.");
    }

    if (!isValidEmail(data.Email)) {
        errors.push("Enter a valid email (e.g., user@example.com).");
    }

    if (!isValidPassword(data.Password)) {
        errors.push("Password must be at least 6 characters.");
    }

    if (data.Password !== data.ConfirmPassword) {
        errors.push("Passwords do not match.");
    }

    if (!isValidName(data.FirstName) || !isValidName(data.LastName)) {
        errors.push("First and last names can only contain letters and cannot exceed 50 characters.");
    }

    if (!isValidPhone(data.Phone)) {
        errors.push("Invalid phone format. Example: +1234567890");
    }

    if (!isValidAddress(data.Address)) {
        errors.push("Address must be between 6 and 200 characters.");
    }

    if (data.RegistrationCode.trim().length > 0 && data.RegistrationCode.length < 4) {
        errors.push("Verification code must be at least 4 characters.");
    }

    return errors;
}

// Функция отправки AJAX-запроса
async function sendRequest(endpoint, data, errorElement) {
    errorElement.innerText = "";

    try {
        const response = await fetch(`${AUTH_SERVICE_URL}${endpoint}`, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            credentials: "include",
            body: JSON.stringify(data)
        });

        const result = await response.json();

        if (!response.ok) {
            throw new Error(result.message || "Request failed.");
        }

        if (result.success) {
            if (result.redirectUrl) {
                window.location.href = result.redirectUrl;
            } else {
                console.log("Login successful, but no redirect URL provided.");
            }
        } else {
            errorElement.innerText = result.message;
        }
    } catch (error) {
        console.error("Error:", error);
        errorElement.innerText = error.message || "An error occurred.";
    }
}

// Обработчик входа (Sign In)
document.getElementById("signInForm").addEventListener("submit", function (event) {
    event.preventDefault();

    const formData = {
        Identifier: document.getElementById("login-identifier").value.trim(),
        Password: document.getElementById("login-password").value.trim()
    };

    const errors = validateSignIn(formData);
    if (errors.length > 0) {
        document.getElementById("login-error-message").innerText = errors.join("\n");
        return;
    }

    sendRequest("/Auth/SignIn", formData, document.getElementById("login-error-message"));
});

// Обработчик регистрации (Sign Up)
document.getElementById("signUpForm").addEventListener("submit", function (event) {
    event.preventDefault();

    const formData = {
        Login: document.getElementById("register-login").value.trim(),
        Email: document.getElementById("register-email").value.trim(),
        Password: document.getElementById("register-password").value.trim(),
        ConfirmPassword: document.getElementById("register-confirm-password").value.trim(),
        FirstName: document.getElementById("register-firstname").value.trim(),
        LastName: document.getElementById("register-lastname").value.trim(),
        Phone: document.getElementById("register-phone").value.trim(),
        Address: document.getElementById("register-address").value.trim(),
        RegistrationCode: document.getElementById("register-code").value.trim()
    };

    const errors = validateSignUp(formData);
    if (errors.length > 0) {
        document.getElementById("register-error-message").innerText = errors.join("\n");
        return;
    }

    sendRequest("/Auth/SignUp", formData, document.getElementById("register-error-message"));
});
