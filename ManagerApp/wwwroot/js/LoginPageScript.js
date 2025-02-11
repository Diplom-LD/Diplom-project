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

        if (!response.ok) {
            throw new Error(`HTTP error! Status: ${response.status}`);
        }

        const result = await response.json();
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
        errorElement.innerText = "An error occurred, please try again.";
    }
}

document.getElementById("signInForm").addEventListener("submit", function (event) {
    event.preventDefault();

    const formData = {
        Identifier: document.getElementById("login-identifier").value,
        Password: document.getElementById("login-password").value
    };

    sendRequest("/Auth/SignIn", formData, document.getElementById("login-error-message"));
});

document.getElementById("signUpForm").addEventListener("submit", function (event) {
    event.preventDefault();

    const formData = {
        Login: document.getElementById("register-login").value,
        Email: document.getElementById("register-email").value,
        Password: document.getElementById("register-password").value,
        ConfirmPassword: document.getElementById("register-confirm-password").value,
        FirstName: document.getElementById("register-firstname").value,
        LastName: document.getElementById("register-lastname").value,
        Phone: document.getElementById("register-phone").value,
        Address: document.getElementById("register-address").value,
        RegistrationCode: document.getElementById("register-code").value
    };

    sendRequest("/Auth/SignUp", formData, document.getElementById("register-error-message"));
});

