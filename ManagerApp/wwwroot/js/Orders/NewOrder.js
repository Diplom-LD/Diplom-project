document.addEventListener("DOMContentLoaded", async function () {
    const equipmentSource = document.getElementById("EquipmentSource");
    const warehouseFields = document.getElementById("warehouseFields");
    const storeFields = document.getElementById("storeFields");

    const warehouseModel = document.getElementById("WarehouseModel");
    const filterSelect = document.getElementById("Filter");
    const sortOrderSelect = document.getElementById("SortOrder");

    const btuField = document.getElementById("BTU");
    const serviceAreaField = document.getElementById("ServiceArea");
    const storePriceField = document.getElementById("StorePrice");
    const quantityField = document.getElementById("RequestedQuantity");

    const storeModelName = document.getElementById("StoreModelName");
    const storeName = document.getElementById("StoreName");
    const modelUrl = document.getElementById("ModelUrl");
    const storeQuantity = document.getElementById("StoreQuantity");

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
        if (equipmentSource.value === "Warehouse") {
            warehouseFields.classList.remove("hidden");
            storeFields.classList.add("hidden");

            storeModelName.value = "";
            storeName.value = "";
            modelUrl.value = "";
            btuField.value = "";
            serviceAreaField.value = "";
            storePriceField.value = "";
            storeQuantity.value = "";

            applySortAndFilter();
        } else {
            warehouseFields.classList.add("hidden");
            storeFields.classList.remove("hidden");

            warehouseModel.selectedIndex = 0;
            quantityField.value = "";
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
            return;
        }
        btuField.value = selectedOption.dataset.btu;
        serviceAreaField.value = selectedOption.dataset.serviceArea;
        storePriceField.value = selectedOption.dataset.price;
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
});
