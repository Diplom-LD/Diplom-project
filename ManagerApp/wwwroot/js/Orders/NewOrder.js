document.addEventListener("DOMContentLoaded", function () {
    const sourceSelect = document.getElementById("EquipmentSource");
    const openPopupBtn = document.getElementById("openPopupBtn");
    const popupIcon = openPopupBtn.querySelector("ion-icon");

    const btuPopup = document.getElementById("btuPopup");
    const warehousePopup = document.getElementById("warehousePopup");

    const conditionersTableBody = document.getElementById("conditionersTableBody");
    const warehouseTableBody = document.getElementById("conditionersTableWarehouseBody");

    const modelNameField = document.getElementById("ModelName");
    const sourceField = document.getElementById("SourceName");
    const modelUrlField = document.getElementById("ModelUrl");
    const btuField = document.getElementById("BTU");
    const serviceAreaField = document.getElementById("ServiceArea");
    const priceField = document.getElementById("Price");
    const quantityField = document.getElementById("Quantity");

    let warehouseEquipmentList = [];
    let warehouseSortingInitialized = false;
    let selectedWarehouseModel = null;
    let maxWarehouseQuantity = null;

    filterValue.addEventListener("input", () => {
        const field = filterField.value;
        const value = parseFloat(filterValue.value);

        if (isNaN(value)) {
            updateWarehouseTable(warehouseEquipmentList);
            return;
        }

        const filtered = warehouseEquipmentList.filter(item => {
            const itemValue = parseFloat(item[field]);
            return !isNaN(itemValue) && itemValue >= value;
        });

        updateWarehouseTable(filtered);
    });

    filterField.addEventListener("change", () => {
        const event = new Event('input');
        filterValue.dispatchEvent(event);
    });


    /* Проверка на количество warehouse */
    quantityField.addEventListener("input", () => {
        const current = parseInt(quantityField.value);
        if (maxWarehouseQuantity !== null && current > maxWarehouseQuantity) {
            quantityField.value = maxWarehouseQuantity;
        }
    });

    async function fetchWarehouseEquipment() {
        try {
            const res = await fetch("/equipment/all-warehouses");
            if (!res.ok) throw new Error("Failed to load equipment");
            warehouseEquipmentList = await res.json();
        } catch (err) {
            console.error("❌ Ошибка загрузки склада:", err);
            warehouseEquipmentList = [];
        }
    }

    function updateButtonIcon() {
        const source = sourceSelect.value;
        popupIcon.setAttribute("name", source === "Store" ? "calculator-outline" : "cube-outline");
    }

    async function togglePopup() {
        const source = sourceSelect.value;

        if (!btuPopup.classList.contains("hidden") || !warehousePopup.classList.contains("hidden")) {
            closeAllPopups();
            return;
        }

        if (source === "Store") {
            btuPopup.classList.remove("hidden");
        } else if (source === "Warehouse") {
            await fetchWarehouseEquipment();
            updateWarehouseTable(warehouseEquipmentList);

            if (!warehouseSortingInitialized) {
                setupWarehouseTableSorting();
                warehouseSortingInitialized = true;
            }

            warehousePopup.classList.remove("hidden");
        }

        popupIcon.setAttribute("name", "close-outline");
        openPopupBtn.style.backgroundColor = "#dc3545";
    }


    function closeAllPopups() {
        btuPopup.classList.add("hidden");
        warehousePopup.classList.add("hidden");
        updateButtonIcon();
        openPopupBtn.style.backgroundColor = "#2a2185";

        warehouseSortingInitialized = false; 
    }


    function fillEquipmentFields({ modelName, sourceName, url, price, btu, area, maxQuantity = null }) {
        modelNameField.value = modelName;
        sourceField.value = sourceName;
        modelUrlField.value = url && url.trim() !== "" ? url : "-";
        priceField.value = price;
        btuField.value = btu;
        serviceAreaField.value = area;
        quantityField.value = "1";

        if (sourceName === "Warehouse" && maxQuantity !== null) {
            maxWarehouseQuantity = maxQuantity;
            quantityField.setAttribute("max", maxWarehouseQuantity);
        } else {
            maxWarehouseQuantity = null;
            quantityField.removeAttribute("max");
        }

        closeAllPopups();
    }


    function handleStoreSelection(event) {
        const row = event.target.closest("tr");
        if (!row) return;

        conditionersTableBody.querySelectorAll("tr").forEach(r => r.classList.remove("selected-row"));
        row.classList.add("selected-row");

        const [modelCell, priceCell, btuCell, areaCell, storeCell] = row.children;
        const link = modelCell.querySelector("a");

        fillEquipmentFields({
            modelName: link?.innerText.trim() || modelCell.innerText.trim(),
            sourceName: storeCell.innerText.trim(),
            url: link?.href || "",
            price: priceCell.innerText.trim().replace(/[^\d]/g, ""),
            btu: btuCell.innerText.trim().replace(/[^\d]/g, ""),
            area: areaCell.innerText.trim().replace(/[^\d]/g, "")
        });
    }

    function handleWarehouseSelection(event) {
        const row = event.target.closest("tr");
        if (!row) return;

        warehouseTableBody.querySelectorAll("tr").forEach(r => r.classList.remove("selected-row"));
        row.classList.add("selected-row");

        const [modelCell, priceCell, btuCell, areaCell, quantityCell] = row.children;
        selectedWarehouseModel = modelCell.innerText.trim(); 

        fillEquipmentFields({
            modelName: selectedWarehouseModel,
            sourceName: "Warehouse",
            url: "",
            price: priceCell.innerText.trim().replace(/[^\d]/g, ""),
            btu: btuCell.innerText.trim().replace(/[^\d]/g, ""),
            area: areaCell.innerText.trim().replace(/[^\d]/g, ""),
            maxQuantity: parseInt(quantityCell.innerText.trim())
        });
    }
    function updateWarehouseTable(products) {
        warehouseTableBody.innerHTML = "";
        products.forEach(product => {
            const row = document.createElement("tr");
            row.innerHTML = `
            <td>${product.modelName}</td>
            <td>${product.price}</td>
            <td>${product.btu}</td>
            <td>${product.serviceArea}</td>
            <td>${product.totalQuantity}</td>
        `;

            if (product.modelName === selectedWarehouseModel) {
                row.classList.add("selected-row");
            }

            warehouseTableBody.appendChild(row);
        });
    }

    function getFilteredProducts() {
        const field = filterField.value;
        const value = parseFloat(filterValue.value);

        if (isNaN(value)) {
            return [...warehouseEquipmentList];
        }

        return warehouseEquipmentList.filter(item => {
            const itemValue = parseFloat(item[field]);
            return !isNaN(itemValue) && itemValue >= value;
        });
    }


    function setupWarehouseTableSorting() {
        const table = document.getElementById("conditionersTableWarehouse");
        const headers = table.querySelectorAll("thead th");

        const fields = ["modelName", "price", "btu", "serviceArea", "totalQuantity"];
        let originalData = [...warehouseEquipmentList]; 

        headers.forEach((header, index) => {
            header.style.cursor = "pointer";

            if (!header.querySelector(".sort-icon")) {
                header.innerHTML += ` <span class="sort-icon">⬍</span>`;
            }

            header.replaceWith(header.cloneNode(true)); 
        });

        const newHeaders = table.querySelectorAll("thead th");

        newHeaders.forEach((header, index) => {
            header.dataset.sortOrder = "none";
            header.addEventListener("click", () => {
                let current = header.dataset.sortOrder;
                let next = current === "none" ? "asc" : current === "asc" ? "desc" : "none";

                newHeaders.forEach(h => {
                    h.dataset.sortOrder = "none";
                    const icon = h.querySelector(".sort-icon");
                    if (icon) icon.textContent = "⬍";
                });

                header.dataset.sortOrder = next;
                const icon = header.querySelector(".sort-icon");
                if (icon) icon.textContent = next === "asc" ? "⬆️" : next === "desc" ? "⬇️" : "⬍";

                const field = fields[index];

                let filteredProducts = getFilteredProducts();

                if (next === "none") {
                    updateWarehouseTable(filteredProducts);
                } else {
                    const isAsc = next === "asc";
                    filteredProducts.sort((a, b) => {
                        if (typeof a[field] === "number") {
                            return isAsc ? a[field] - b[field] : b[field] - a[field];
                        } else {
                            return isAsc
                                ? String(a[field]).localeCompare(String(b[field]))
                                : String(b[field]).localeCompare(String(a[field]));
                        }
                    });
                    updateWarehouseTable(filteredProducts);
                }
            });
        });
    }

    function handleEquipmentSourceChange() {
        modelNameField.value = "";
        sourceField.value = "";
        modelUrlField.value = "";
        btuField.value = "";
        serviceAreaField.value = "";
        priceField.value = "";
        quantityField.value = "";

        document.querySelectorAll("#conditionersTableBody tr.selected-row, #conditionersTableWarehouseBody tr.selected-row")
            .forEach(row => row.classList.remove("selected-row"));

        maxWarehouseQuantity = null;
        selectedWarehouseModel = null;
        quantityField.removeAttribute("max");

        if (sourceSelect.value === "Warehouse") {
            modelUrlField.value = "-";
        }

        updateButtonIcon();
        closeAllPopups();
    }


    sourceSelect.addEventListener("change", handleEquipmentSourceChange);
    sourceSelect.addEventListener("change", updateButtonIcon);
    openPopupBtn.addEventListener("click", async () => await togglePopup());
    btuPopup.addEventListener("click", e => { if (e.target === btuPopup) closeAllPopups(); });
    warehousePopup.addEventListener("click", e => { if (e.target === warehousePopup) closeAllPopups(); });
    conditionersTableBody.addEventListener("click", handleStoreSelection);
    warehouseTableBody.addEventListener("click", handleWarehouseSelection);

    updateButtonIcon();

    // ---- Создание заявки ----
    const technicianSelect = document.getElementById("Technicians");
    const technicianMode = document.getElementById("TechnicianSelection");
    const manualBlock = document.getElementById("manualTechnicianSelection");
    const selectedTechniciansContainer = document.getElementById("selectedTechniciansContainer");

    technicianMode.addEventListener("change", async () => {
        if (technicianMode.value === "manual") {
            manualBlock.classList.remove("hidden");
            await loadTechnicians();
        } else {
            manualBlock.classList.add("hidden");
            resetTechnicianSelection();
        }
    });

    async function loadTechnicians() {
        try {
            const response = await fetch("/technicians/available-today");
            if (!response.ok) throw new Error("Ошибка загрузки техников");

            const technicians = await response.json();
            technicianSelect.innerHTML = "";

            technicians.forEach(t => {
                const option = document.createElement("option");
                option.value = t.id;
                option.textContent = t.fullName;
                technicianSelect.appendChild(option);
            });

            setupMultiSelect();
        } catch (err) {
            console.error("❌ Ошибка загрузки техников:", err);
        }
    }

    function resetTechnicianSelection() {
        technicianSelect.innerHTML = "";
        selectedTechniciansContainer.innerHTML = "";
    }

    function setupMultiSelect() {
        selectedTechniciansContainer.innerHTML = "";
        technicianSelect.addEventListener("change", updateTags);
        updateTags();
    }

    function updateTags() {
        selectedTechniciansContainer.innerHTML = "";
        Array.from(technicianSelect.selectedOptions).forEach(option => {
            const tag = document.createElement("div");
            tag.classList.add("technician-tag");
            tag.textContent = option.textContent;

            const removeBtn = document.createElement("span");
            removeBtn.classList.add("remove-technician");
            removeBtn.textContent = "×";
            removeBtn.addEventListener("click", () => {
                option.selected = false;
                updateTags();
            });

            tag.appendChild(removeBtn);
            selectedTechniciansContainer.appendChild(tag);
        });
    }

    const orderForm = document.getElementById("orderForm");

    orderForm.addEventListener("submit", async function (e) {
        e.preventDefault();

        console.log("📋 Notes value before payload:", document.getElementById("Notes").value);

        const payload = {
            orderType: document.getElementById("OrderType").value,
            installationDate: new Date(document.getElementById("InstallationDate").value).toISOString(),
            installationAddress: document.getElementById("AddressInstallation").value,
            notes: document.getElementById("Notes").value,
            workCost: parseFloat(document.getElementById("WorkCost").value),
            paymentMethod: document.getElementById("PaymentMethod").value,
            paymentStatus: "UnPaid",
            fulfillmentStatus: "New",
            fullName: document.getElementById("FullName").value,
            phoneNumber: document.getElementById("PhoneNumber").value,
            email: document.getElementById("Email").value,
            technicianIds: technicianMode.value === "manual" ? Array.from(technicianSelect.selectedOptions).map(opt => opt.value) : []
        };

        if (payload.orderType === "Installation") {
            const modelName = document.getElementById("ModelName").value;
            const source = sourceSelect.value;
            const modelUrl = document.getElementById("ModelUrl").value;
            const btu = parseInt(document.getElementById("BTU").value);
            const serviceArea = parseFloat(document.getElementById("ServiceArea").value);
            const price = parseFloat(document.getElementById("Price").value);
            const quantity = parseInt(document.getElementById("Quantity").value);

            if (!modelName || !price || !btu || !serviceArea || !quantity) {
                alert("❌ Заполните все поля оборудования.");
                return;
            }

            if (source === "Store" && !modelUrl) {
                alert("❌ Укажите ссылку на модель из магазина.");
                return;
            }

            payload.equipment = {
                modelName,
                modelSource: source,
                modelUrl: source === "Store" ? modelUrl : null,
                btu,
                serviceArea,
                price,
                quantity
            };
        }

        try {
            const response = await fetch("/manager/orders/create", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": document.querySelector("input[name='__RequestVerificationToken']")?.value || ""
                },
                body: JSON.stringify(payload)
            });

            if (!response.ok) {
                const err = await response.json();
                console.error("❌ Ошибка при создании:", err);
                alert("Ошибка при создании заявки. Проверьте введённые данные.");
                return;
            }

            const result = await response.json();
            window.location.href = `/orders/details/${result.orderId}`;
        } catch (err) {
            console.error("❌ Сетевая ошибка:", err);
            alert("Сетевая ошибка при создании заявки.");
        }
    });
});