document.addEventListener("DOMContentLoaded", () => {
    let warehouses = [];
    let editingId = null;

    const grid = document.getElementById("warehouseGrid");
    const searchInput = document.getElementById("searchInput");
    const gridSelect = document.getElementById("gridSelect");
    const modal = document.getElementById("warehouseModal");
    const form = document.getElementById("warehouseForm");
    const modalTitle = document.getElementById("modalTitle");
    const deleteButton = document.getElementById("deleteButton");
    const addButton = document.querySelector(".add-button");
    const cancelButton = document.querySelector(".cancel-button");

    async function fetchWarehouses() {
        try {
            const response = await fetch("/Warehouses/GetAllWarehouses");
            warehouses = await response.json();
            renderWarehouses();
            applyResponsiveGrid();
        } catch (error) {
            console.error("Ошибка загрузки складов:", error);
        }
    }

    function formatDate(dateStr) {
        const date = new Date(dateStr);
        const day = String(date.getDate()).padStart(2, '0');
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const year = date.getFullYear();
        const hours = String(date.getHours()).padStart(2, '0');
        const minutes = String(date.getMinutes()).padStart(2, '0');

        return `${day}.${month}.${year} ${hours}:${minutes}`;
    }

    function renderWarehouses() {
        const searchTerm = searchInput.value.toLowerCase();
        const filtered = warehouses.filter(w =>
            Object.values(w).some(value =>
                String(value).toLowerCase().includes(searchTerm)
            )
        );

        grid.innerHTML = "";

        if (filtered.length === 0) {
            grid.innerHTML = `<p class="empty-state">No warehouses found matching your search.</p>`;
            return;
        }

        filtered.forEach((w, index) => {
            const card = document.createElement("div");
            card.className = "warehouse-card";
            card.style.animationDelay = `${index * 100}ms`;
            card.innerHTML = `
            <div class="card-content">
                <div class="card-header">
                    <h2><a href="/Warehouses/WarehouseDetails/${w.id}" class="warehouse-link">${w.name}</a></h2>
                    <button class="edit-button" data-id="${w.id}">✏️</button>
                </div>
                <p><strong>Address:</strong> ${w.address}</p>
                <p><strong>Contact:</strong> ${w.contactPerson}</p>
                <p><strong>Phone:</strong> ${w.phoneNumber}</p>
                <p><strong>Last Inventory Check:</strong> ${formatDate(w.lastInventoryCheck)}</p>
            </div>
            <div class="card-actions">
                <button class="go-button" data-id="${w.id}">View</button>
            </div>
        `;
            grid.appendChild(card);
        });
    }

    function applyResponsiveGrid() {
        let cols = parseInt(gridSelect.value, 10);
        const screenWidth = window.innerWidth;
        if (screenWidth <= 768) cols = 1;
        else if (screenWidth <= 1024 && cols > 2) cols = 2;

        grid.style.gridTemplateColumns = `repeat(${cols}, 1fr)`;

        const cards = grid.querySelectorAll(".warehouse-card");
        cards.forEach((card, index) => {
            card.style.animation = "none";
            void card.offsetWidth;
            card.style.animation = "fadeInForward 0.5s ease forwards";
            card.style.animationDelay = `${index * 100}ms`;
        });
    }

    function openModal(id = null) {
        modal.classList.remove("hidden");
        form.reset();
        editingId = id;

        if (id) {
            const warehouse = warehouses.find(w => w.id === id);
            modalTitle.textContent = "Edit Warehouse";
            document.getElementById("modalName").value = warehouse.name;
            document.getElementById("modalAddress").value = warehouse.address;
            document.getElementById("modalContact").value = warehouse.contactPerson;
            document.getElementById("modalPhone").value = warehouse.phoneNumber;

            const date = new Date(warehouse.lastInventoryCheck);
            const offsetDate = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
            document.getElementById("modalDateTime").value = offsetDate.toISOString().slice(0, 16);

            deleteButton.classList.remove("hidden");
        } else {
            modalTitle.textContent = "Add Warehouse";
            deleteButton.classList.add("hidden");
        }
    }

    function closeModal() {
        modal.classList.add("hidden");
        clearModalFields();
    }

    async function deleteWarehouse() {
        if (!editingId) return;
        if (!confirm("Are you sure you want to delete this warehouse?")) return;

        const res = await fetch(`/Warehouses/DeleteWarehouse?id=${editingId}`, {
            method: "DELETE"
        });

        if (res.ok) {
            await fetchWarehouses();
            closeModal();
        }
    }

    async function handleFormSubmit(e) {
        e.preventDefault();

        const name = document.getElementById("modalName").value.trim();
        const address = document.getElementById("modalAddress").value.trim();
        const contactPerson = document.getElementById("modalContact").value.trim();
        const phoneNumber = document.getElementById("modalPhone").value.trim();
        const lastInventoryCheck = document.getElementById("modalDateTime").value;
        const phoneRegex = /^\+?[1-9]\d{7,14}$/;

        if (!name || name.length > 100) return;
        if (!address || address.length > 200) return;
        if (!contactPerson || contactPerson.length > 50) return;
        if (!phoneRegex.test(phoneNumber)) return;
        if (!lastInventoryCheck) return;

        const warehouse = {
            name,
            address,
            contactPerson,
            phoneNumber,
            lastInventoryCheck: new Date(lastInventoryCheck).toISOString(),
            latitude: 0,
            longitude: 0
        };

        const isEditing = !!editingId;

        if (isEditing) {
            const existingWarehouse = warehouses.find(w => w.id === editingId);
            if (existingWarehouse) {
                const unchanged =
                    existingWarehouse.name === warehouse.name &&
                    existingWarehouse.address === warehouse.address &&
                    existingWarehouse.contactPerson === warehouse.contactPerson &&
                    existingWarehouse.phoneNumber === warehouse.phoneNumber &&
                    new Date(existingWarehouse.lastInventoryCheck).toISOString().slice(0, 16) === new Date(lastInventoryCheck).toISOString().slice(0, 16);

                if (unchanged) {
                    closeModal();
                    return;
                }
            }
        }

        const url = editingId
            ? `/Warehouses/UpdateWarehouse?id=${editingId}`
            : "/Warehouses/AddWarehouse";

        const method = editingId ? "PUT" : "POST";

        try {
            const res = await fetch(url, {
                method,
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(warehouse)
            });

            if (res.ok) {
                await fetchWarehouses();
                closeModal();
            } else {
                let message = "Ошибка при сохранении склада.";
                try {
                    const contentType = res.headers.get("Content-Type");
                    if (contentType && contentType.includes("application/json")) {
                        const data = await res.json();
                        message = (data.message || message).replace(/\(Parameter '.*?'\)/, "").trim();
                    } else {
                        message = await res.text();
                        message = message.replace(/\(Parameter '.*?'\)/, "").trim();
                    }
                } catch (parseErr) {
                    console.error("Ошибка при парсинге ошибки:", parseErr);
                }

                alert(`Ошибка при сохранении склада:\n${message}`);
            }
        } catch (error) {
            console.error("Ошибка запроса:", error);
            alert("Произошла ошибка сети или сервера. Проверьте соединение.");
        }
    }

    function updateGridOptionsForScreen() {
        const screenWidth = window.innerWidth;
        const currentValue = parseInt(gridSelect.value, 10) || 1;

        let allowedOptions = [1, 2, 3, 4];
        if (screenWidth <= 768) allowedOptions = [1];
        else if (screenWidth <= 1024) allowedOptions = [1, 2];
        else if (screenWidth <= 1300) allowedOptions = [1, 2, 3];

        gridSelect.innerHTML = "";
        allowedOptions.forEach(val => {
            const option = document.createElement("option");
            option.value = val;
            option.textContent = `${val}x${val}`;
            option.selected = val === currentValue || !allowedOptions.includes(currentValue);
            gridSelect.appendChild(option);
        });
    }

    deleteButton.addEventListener("click", deleteWarehouse);
    addButton.addEventListener("click", () => openModal());
    cancelButton.addEventListener("click", closeModal);
    form.addEventListener("submit", handleFormSubmit);
    gridSelect.addEventListener("change", applyResponsiveGrid);
    window.addEventListener("resize", () => {
        updateGridOptionsForScreen();
        applyResponsiveGrid();
    });
    searchInput.addEventListener("input", () => {
        renderWarehouses();
        applyResponsiveGrid();
    });

    grid.addEventListener("click", e => {
        if (e.target.classList.contains("edit-button")) {
            openModal(e.target.dataset.id);
        }

        if (e.target.classList.contains("go-button")) {
            const id = e.target.dataset.id;
            if (id) window.location.href = `/Warehouses/WarehouseDetails/${id}`;
        }
    });

    function clearModalFields() {
        form.reset(); 
        editingId = null;
    }

    updateGridOptionsForScreen();
    fetchWarehouses();
});
