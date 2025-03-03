document.addEventListener("DOMContentLoaded", async function () {
    const form = document.getElementById("searchConditionersForm");
    const searchType = document.getElementById("searchType");
    let searchValue = document.getElementById("searchValue");
    const messageContainer = document.getElementById("conditionersMessage");
    const rangeMessageContainer = document.getElementById("rangeMessageContainer");
    const table = document.getElementById("conditionersTable");
    const tableBody = document.getElementById("conditionersTableBody");
    let originalOrder = [];
    let currentSortColumn = null;
    let sortState = {};
    const originalInput = searchValue.cloneNode(true);

    if (form) {
        form.addEventListener("submit", async function (event) {
            event.preventDefault();
            clearMessages();
            await searchConditioners();
        });
    }

    searchType.addEventListener("change", async function () {
        const selectedType = searchType.value;
        const parent = searchValue.parentElement;
        const newInput = originalInput.cloneNode(true);
        newInput.id = "searchValue";
        newInput.value = "";

        if (selectedType === "store") {
            const stores = await fetchStores();
            if (stores.length > 0) {
                const select = document.createElement("select");
                select.id = "searchValue";
                select.className = "search-dropdown";
                select.innerHTML = stores.map(store => `<option value="${store}">${store}</option>`).join("");
                parent.replaceChild(select, searchValue);
                searchValue = select;
                return;
            }
        }

        parent.replaceChild(newInput, searchValue);
        searchValue = newInput;
    });

    function clearMessages() {
        messageContainer.style.display = "none";
        messageContainer.innerHTML = "";
        rangeMessageContainer.style.display = "none";
        rangeMessageContainer.innerHTML = "";
        table.style.display = "none";
        tableBody.innerHTML = "";

        const successMessageContainer = document.getElementById("successMessage");
        successMessageContainer.style.display = "none";
        successMessageContainer.innerHTML = "";
    }


    function showMessage(message) {
        messageContainer.style.display = "block";
        messageContainer.innerHTML = message;
    }


    async function fetchStores() {
        try {
            const response = await fetch("/BTU/products/stores");
            if (!response.ok) throw new Error("Error loading stores");

            const text = await response.text();
            if (!text.trim()) return []; 

            const data = JSON.parse(text);
            return Array.isArray(data.stores) ? data.stores.sort((a, b) => a.localeCompare(b, undefined, { sensitivity: 'base' })) : [];
        } catch (error) {
            console.error("❌ Error when loading stores:", error);
            return [];
        }
    }

    async function searchConditioners() {
        const selectedType = searchType.value;
        const value = searchValue.value.trim();

        if (!value) {
            showMessage("⚠️  Enter a value for the search.");
            return;
        }

        let endpoint;
        let numericValue = selectedType === "price" ? parseFloat(value) : parseInt(value, 10);

        if (selectedType !== "store") {
            if (isNaN(numericValue) || numericValue <= 0) {
                showMessage(`❌ Error: ${selectedType.toUpperCase()} must be a positive number.`);
                return;
            }
        }

        if (selectedType === "store") {
            const invalidChars = /[<>$%^&*()]/;
            if (invalidChars.test(value)) {
                showMessage("❌ Error: Input contains prohibited characters.");
                return;
            }
        }

        if (selectedType === "btu" && (numericValue < 1000 || numericValue > 300000)) {
            showMessage("❌ Error: BTU should be between 1000 and 300000.");
            return;
        }

        if (selectedType === "price") {
            if (numericValue < 1000 || numericValue > 500000) {
                showMessage("❌ Error: The price should be between 1,000 and 500,000.");
                return;
            }
            numericValue = Math.min(numericValue, 500000);
        }

        if (selectedType === "service_area" && (numericValue < 10 || numericValue > 500)) {
            showMessage("❌ Error: The area should be between 10 and 500 m².");
            return;
        }

        if (selectedType === "store") {
            const stores = await fetchStores();
            if (stores.length === 0) {
                showMessage("❌ Error: The list of stores has not been loaded. Try again later.");
                return;
            }

            if (!stores.some(store => store.trim().toLowerCase() === value.toLowerCase())) {
                showMessage(`❌ Error: Store "${value}" is not on the list.`);
                return;
            }
        }

        switch (selectedType) {
            case "btu":
                endpoint = `/BTU/products/range/?btu_min=${numericValue}&btu_max=${numericValue + 1000}`;
                break;
            case "price":
                let priceMax = Math.min(numericValue + 1000, 500000);
                endpoint = `/BTU/products/price/?price_min=${numericValue}&price_max=${priceMax}`;
                break;
            case "service_area":
                endpoint = `/BTU/products/service_area/${numericValue}`;
                break;
            case "store":
                endpoint = `/BTU/products/store/${value}`;
                break;
        }

        try {
            const response = await fetch(endpoint);

            if (!response.ok) {
                let errorText = `❌ Request error (${response.status})`;
                if (response.status === 404) errorText = "❌ Goods not found.";
                if (response.status === 400) errorText = "⚠️ Incorrect query parameters.";
                if (response.status === 500) errorText = "🚨 Server error, try again later.";

                throw new Error(errorText);
            }

            const products = await response.json();
            updateConditionersTable(products);
        } catch (error) {
            console.error("❌ Search Error:", error);
            showMessage(error.message);
        }
    }

    function updateConditionersTable(products) {
        clearMessages();

        if (!Array.isArray(products) || products.length === 0) {
            showMessage("❌ There are no air conditioners available");
            return;
        }

        products = products.filter(p => p.btu && p.price && p.service_area);
        if (products.length === 0) {
            showMessage("❌ There are no conditioners with correct data.");
            return;
        }

        messageContainer.style.display = "none";
        table.style.display = "table";

        tableBody.innerHTML = products.map(product => `
        <tr>
            <td><a href="${product.url}" target="_blank">${product.name || "—"}</a></td>
            <td>${product.price ? `${parseFloat(product.price)} ${product.currency || "—"}` : "—"}</td>
            <td>${product.btu ? `${parseInt(product.btu, 10)} BTU` : "—"}</td>
            <td>${product.service_area ? `${product.service_area} m²` : "—"}</td>
            <td>${product.store || "—"}</td>
        </tr>`).join("");

        originalOrder = Array.from(tableBody.querySelectorAll("tr"));
        setupTableSorting();

        const successMessageContainer = document.getElementById("successMessage");
        successMessageContainer.style.display = "block";
        successMessageContainer.innerHTML = `✅ Air conditioners found: <strong>${products.length}</strong>`;
    }



    function setupTableSorting() {
        document.querySelectorAll("#conditionersTable thead td").forEach((header, index) => {
            if (!header.querySelector(".sort-icon")) {
                header.style.cursor = "pointer";
                const icon = document.createElement("span");
                icon.classList.add("sort-icon");
                icon.textContent = " ⬍";
                header.appendChild(icon);
                header.addEventListener("click", () => sortTable(index, header));
            }
        });
    }

    function sortTable(columnIndex, header) {
        const tbody = document.querySelector("#conditionersTable tbody");
        const rows = Array.from(tbody.querySelectorAll("tr"));

        if (!originalOrder.length) {
            originalOrder = [...rows];
        }

        let currentSortOrder = header.dataset.sortOrder;
        let sortOrder = currentSortOrder === "asc" ? "desc" : currentSortOrder === "desc" ? "" : "asc";
        header.dataset.sortOrder = sortOrder;

        document.querySelectorAll(".sort-icon").forEach(icon => icon.textContent = " ⬍");
        if (sortOrder) {
            header.querySelector(".sort-icon").textContent = sortOrder === "asc" ? " ⬆️" : " ⬇️";
        }

        if (!sortOrder) {
            tbody.innerHTML = "";
            originalOrder.forEach(row => tbody.appendChild(row));
            return;
        }

        rows.sort((a, b) => {
            let valA = a.children[columnIndex].textContent.trim();
            let valB = b.children[columnIndex].textContent.trim();

            if (!isNaN(parseFloat(valA)) && !isNaN(parseFloat(valB))) {
                valA = parseFloat(valA);
                valB = parseFloat(valB);
            }

            return sortOrder === "asc" ? valA > valB ? 1 : -1 : valA < valB ? 1 : -1;
        });

        tbody.innerHTML = "";
        rows.forEach(row => tbody.appendChild(row));
    }
});
