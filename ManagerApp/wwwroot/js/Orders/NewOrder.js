document.addEventListener("DOMContentLoaded", async function () {
    const equipmentSource = document.getElementById("EquipmentSource");
    const warehouseFields = document.getElementById("warehouseFields");
    const storeFields = document.getElementById("storeFields");

    const warehouseModel = document.getElementById("WarehouseModel");
    const filterSelect = document.getElementById("Filter");
    const sortOrderSelect = document.getElementById("SortOrder");

    const storeModelName = document.getElementById("StoreModelName");
    const storeName = document.getElementById("StoreName");
    const modelUrl = document.getElementById("ModelUrl");
    const btuField = document.getElementById("BTU");
    const serviceAreaField = document.getElementById("ServiceArea");
    const storePriceField = document.getElementById("StorePrice");
    const storeQuantity = document.getElementById("StoreQuantity");
    const quantityField = document.getElementById("RequestedQuantity");

    const conditionersTableBody = document.getElementById("conditionersTableBody");
    const equipmentSourceSelect = document.getElementById("EquipmentSource");

    let fullEquipmentList = [];

    async function loadEquipment() {
        try {
            const response = await fetch("/equipment/all-warehouses");
            if (!response.ok) throw new Error(`Ошибка загрузки данных. Код: ${response.status}`);

            fullEquipmentList = await response.json();

            applySortAndFilter();
        } catch (error) {
            console.error("❌ Ошибка загрузки оборудования:", error);
            warehouseModel.innerHTML = `<option value="">Ошибка загрузки</option>`;
        }
    }

    function toggleFields() {
        const storeInputs = storeFields.querySelectorAll("[data-required]");
        const warehouseInputs = warehouseFields.querySelectorAll("[data-required]");

        if (equipmentSource.value === "Warehouse") {
            warehouseFields.classList.remove("hidden");
            storeFields.classList.add("hidden");
            storeInputs.forEach(input => input.removeAttribute("required"));
            warehouseInputs.forEach(input => input.setAttribute("required", "required"));

            storeModelName.value = "";
            storeName.value = "";
            modelUrl.value = "";
            storePriceField.value = "";
            storeQuantity.value = "";
            btuField.value = "";
            serviceAreaField.value = "";
            applySortAndFilter();
        } else {
            warehouseFields.classList.add("hidden");
            storeFields.classList.remove("hidden");
            warehouseInputs.forEach(input => input.removeAttribute("required"));
            storeInputs.forEach(input => input.setAttribute("required", "required"));

            warehouseModel.selectedIndex = 0;
            quantityField.value = "";
            btuField.value = "";
            serviceAreaField.value = "";
        }
    }



    function updateModelOptions(list) {
        warehouseModel.innerHTML = `<option value="">Select Model</option>`;
        list.forEach(item => {
            const option = document.createElement("option");
            option.value = item.modelName;
            option.textContent = `${item.modelName} - BTU: ${item.btu} - Area: ${item.serviceArea}m² - Price: ${item.price} MDL - Available: ${item.totalQuantity}`;
            option.dataset.btu = item.btu;
            option.dataset.serviceArea = item.serviceArea;
            option.dataset.price = item.price;
            option.dataset.totalQuantity = item.totalQuantity;
            warehouseModel.appendChild(option);
        });
    }

    function applySortAndFilter() {
        const sortType = filterSelect.value;
        const sortOrder = sortOrderSelect.value;

        let sortedList = [...fullEquipmentList];

        if (sortType === "" || sortType === "All") {
            sortedList.sort((a, b) => sortOrder === "asc"
                ? a.modelName.localeCompare(b.modelName, undefined, { sensitivity: 'base' })
                : b.modelName.localeCompare(a.modelName, undefined, { sensitivity: 'base' })
            );
        } else if (sortType === "BTU") {
            sortedList.sort((a, b) => sortOrder === "asc" ? a.btu - b.btu : b.btu - a.btu);
        } else if (sortType === "ServiceArea") {
            sortedList.sort((a, b) => sortOrder === "asc" ? a.serviceArea - b.serviceArea : b.serviceArea - a.serviceArea);
        } else if (sortType === "Price") {
            sortedList.sort((a, b) => sortOrder === "asc" ? a.price - b.price : b.price - a.price);
        }

        updateModelOptions(sortedList);
    }

    warehouseModel.addEventListener("change", function () {
        const selectedOption = warehouseModel.options[warehouseModel.selectedIndex];

        if (!selectedOption.value) {
            quantityField.value = "";
            return;
        }

        quantityField.max = selectedOption.dataset.totalQuantity;
        quantityField.value = "";
    });


    quantityField.addEventListener("input", function () {
        const maxQuantity = parseInt(quantityField.max, 10);
        if (quantityField.value > maxQuantity) {
            quantityField.value = maxQuantity;
        }
    });

    storeQuantity.addEventListener("input", function () {
        if (storeQuantity.value < 1) {
            storeQuantity.value = 1;
        }
    });

    filterSelect.addEventListener("change", applySortAndFilter);
    sortOrderSelect.addEventListener("change", applySortAndFilter);

    equipmentSource.addEventListener("change", toggleFields);

    toggleFields();
    await loadEquipment();

    /* Technician selection */
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
            console.log("🔄 Загружаем список техников...");
            const response = await fetch("/technicians/available-today");
            if (!response.ok) throw new Error(`Ошибка загрузки. Код: ${response.status}`);

            const technicians = await response.json();

            technicianSelect.innerHTML = ""; 

            if (technicians.length === 0) {
                technicianSelect.innerHTML = `<option disabled>Нет доступных техников</option>`;
                console.warn("⚠️ Нет доступных техников!");
                return;
            }

            technicians.forEach(t => {
                const option = document.createElement("option");
                option.value = t.id;
                option.textContent = t.fullName;
                technicianSelect.appendChild(option);
            });

            console.log(`✅ Загружено техников: ${technicians.length}`);

            if (technicianSelect) {
                setupMultiSelect(); 
            }
        } catch (err) {
            console.error("❌ Ошибка загрузки техников:", err);
            technicianSelect.innerHTML = `<option disabled>Ошибка загрузки</option>`;
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
            const tag = createTag(option);
            selectedTechniciansContainer.appendChild(tag);
        });
    }

    function createTag(option) {
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
        return tag;
    }


    /*BTU Calculator*/
    const openPopupBtn = document.getElementById('openPopupBtn');
    const popupIcon = openPopupBtn.querySelector('ion-icon');
    const btuPopup = document.getElementById('btuPopup');

    function togglePopup() {
        const isHidden = btuPopup.classList.contains('hidden');
        btuPopup.classList.toggle('hidden');
        popupIcon.setAttribute('name', isHidden ? 'close-outline' : 'calculator-outline');
        openPopupBtn.style.backgroundColor = isHidden ? '#dc3545' : '#2a2185';
    }

    function closePopup() {
        btuPopup.classList.add('hidden');
        popupIcon.setAttribute('name', 'calculator-outline');
        openPopupBtn.style.backgroundColor = '#2a2185';
    }

    openPopupBtn.addEventListener('click', togglePopup);

    btuPopup.addEventListener('click', (e) => {
        if (e.target === btuPopup) closePopup();
    });


    /*Выбор кондиционера*/
    function handleConditionerSelection(event) {
        const selectedRow = event.target.closest("tr");
        if (!selectedRow) return;

        const columns = selectedRow.children;
        if (columns.length < 5) return;

        let modelLinkElement = columns[0].querySelector("a");
        let selectedModel = modelLinkElement ? modelLinkElement.innerText.trim() : columns[0].innerText.trim();
        let selectedUrl = modelLinkElement ? modelLinkElement.href : "";

        let selectedPrice = columns[1].innerText.trim().replace(/[^\d]/g, "");
        let selectedBTU = columns[2].innerText.trim().replace(/[^\d]/g, "");
        let selectedServiceArea = columns[3].innerText.trim().replace(/[^\d]/g, "");
        let selectedStore = columns[4].innerText.trim();

        if (equipmentSourceSelect.value === "Store") {
            storeModelName.value = selectedModel;
            storeName.value = selectedStore;
            modelUrl.value = selectedUrl;
            btuField.value = selectedBTU;
            serviceAreaField.value = selectedServiceArea;
            storePriceField.value = selectedPrice;
            storeQuantity.value = "1";

            closePopup();
        }
    }

    conditionersTableBody.addEventListener("click", handleConditionerSelection);


    const orderTypeSelect = document.getElementById("OrderType");
    const equipmentSection = document.getElementById("equipmentSection");
    function toggleEquipmentSection() {
        const selected = orderTypeSelect.value;
        if (selected === "Maintenance") {
            equipmentSection.classList.add("hidden");
        } else {
            equipmentSection.classList.remove("hidden");
        }
    }
    orderTypeSelect.addEventListener("change", toggleEquipmentSection);
    toggleEquipmentSection();


    /* Сreate Order */
    const orderForm = document.getElementById("orderForm");

    orderForm.addEventListener("submit", async function (e) {
        e.preventDefault();

        const orderType = document.getElementById("OrderType").value;
        const workCost = parseFloat(document.getElementById("WorkCost").value);
        const paymentMethod = document.getElementById("PaymentMethod").value;
        const installationDate = new Date(document.getElementById("InstallationDate").value).toISOString();
        const addressInstallation = document.getElementById("AddressInstallation").value;
        const notes = document.getElementById("Notes").value;

        const equipmentSource = equipmentSourceSelect.value;
        let equipment = null;

        if (orderType === "Installation") {
            if (equipmentSource === "Warehouse") {
                const selectedOption = warehouseModel.options[warehouseModel.selectedIndex];
                equipment = {
                    modelName: selectedOption.value,
                    modelSource: "Warehouse",
                    btu: parseInt(selectedOption.dataset.btu),
                    serviceArea: parseFloat(selectedOption.dataset.serviceArea),
                    price: parseFloat(selectedOption.dataset.price),
                    quantity: parseInt(document.getElementById("RequestedQuantity").value)
                };
            } else if (equipmentSource === "Store") {
                equipment = {
                    modelName: storeModelName.value,
                    modelSource: "Store",
                    btu: parseInt(btuField.value),
                    serviceArea: parseFloat(serviceAreaField.value),
                    price: parseFloat(storePriceField.value),
                    quantity: parseInt(storeQuantity.value)
                };
            }
        }

        const client = {
            fullName: document.getElementById("FullName")?.value,
            phoneNumber: document.getElementById("PhoneNumber")?.value,
            email: document.getElementById("Email")?.value
        };

        const technicianMode = document.getElementById("TechnicianSelection").value;
        const selectedTechnicians = Array.from(technicianSelect.selectedOptions).map(opt => opt.value);

        const payload = {
            orderType,
            installationDate,
            installationAddress: addressInstallation,
            notes,
            workCost,
            paymentMethod,
            paymentStatus: "UnPaid",
            fulfillmentStatus: "New",
            equipment,
            fullName: client.fullName,
            phoneNumber: client.phoneNumber,
            email: client.email,
            technicianIds: technicianMode === "manual" ? selectedTechnicians : []
        };

        try {
            const response = await fetch("/manager/orders/create", {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": document.querySelector("input[name='__RequestVerificationToken']")?.value ?? ""
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
