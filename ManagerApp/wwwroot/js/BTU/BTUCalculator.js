document.addEventListener("DOMContentLoaded", function () {
    const form = document.getElementById("btuCalculatorForm");
    const findBtn = document.querySelector(".btu-find-btn");
    const clearBtn = document.querySelector(".btu-clear-btn");

    if (form) {
        form.addEventListener("submit", async function (event) {
            event.preventDefault();
            await calculateBTU();
        });
    }

    if (clearBtn) {
        clearBtn.addEventListener("click", resetCalculator);
    }

    document.getElementById("hasVentilation")?.addEventListener("change", toggleVentilation);
    document.getElementById("hasLargeWindow")?.addEventListener("change", toggleWindowArea);

    if (findBtn) {
        findBtn.addEventListener("click", findConditioners);
    }

    toggleVentilation();
    toggleWindowArea();
    setupTableSorting();
});

/**
 * 🔄 Показывает/скрывает блок дополнительных параметров
 */
function toggleParams() {
    let additionalParams = document.getElementById("additionalParams");
    let toggleCheckbox = document.getElementById("toggleParamsCheckbox");

    additionalParams.style.display = toggleCheckbox.checked ? "block" : "none";
}

/**
 * 🧹 Полностью очищает форму, скрывает результаты, таблицу кондиционеров и сбрасывает доп. параметры
 */
function resetCalculator() {
    const form = document.getElementById("btuCalculatorForm");
    if (form) {
        form.reset();
    }

    document.getElementById("btuResults").style.display = "none";

    const btuHint = document.getElementById("btuHint");
    if (btuHint) {
        btuHint.classList.remove("hidden");
    }

    ["calculatedBTU", "calculatedPowerKW", "recommendedBTULower", "recommendedBTUUpper", "recommendedKWLower", "recommendedKWUpper"]
        .forEach(id => {
            const el = document.getElementById(id);
            if (el) el.innerText = "—";
        });

    const findBtn = document.querySelector(".btu-find-btn");
    if (findBtn) {
        findBtn.dataset.btuMin = "";
        findBtn.dataset.btuMax = "";
        findBtn.disabled = true;
    }

    clearConditionersTable();

    const additionalParams = document.getElementById("additionalParams");
    if (additionalParams) {
        additionalParams.style.display = "none";
    }

    toggleVentilation();
    toggleWindowArea();

    console.log("🔄 Форма, таблица и дополнительные параметры очищены!");
}

/**
 * 🧹 Очищает таблицу кондиционеров и скрывает сообщение о экстремумах
 */
function clearConditionersTable() {
    const table = document.getElementById("conditionersTable");
    const tableBody = document.getElementById("conditionersTableBody");
    const messageContainer = document.getElementById("conditionersMessage");
    const rangeMessageContainer = document.getElementById("rangeMessageContainer"); 

    if (tableBody) {
        tableBody.innerHTML = "";
    }

    if (table) {
        table.style.display = "none";
    }

    if (messageContainer) {
        messageContainer.style.display = "block";
        messageContainer.innerHTML = "⚠️ Сначала выполните расчет BTU!";
    }

    if (rangeMessageContainer) {
        rangeMessageContainer.style.display = "none";
    }
}

/**
 * 🔄 Универсальная функция для скрытия/отображения блоков
 */
function toggleVisibility(inputId, groupId) {
    const input = document.getElementById(inputId);
    const group = document.getElementById(groupId);
    if (input && group) {
        group.style.display = input.checked ? 'block' : 'none';
    }
}

function toggleVentilation() {
    const ventilationSelect = document.getElementById("hasVentilation");
    const airExchangeGroup = document.getElementById("airExchangeRateGroup");
    const airExchangeInput = document.getElementById("airExchangeRate");

    if (ventilationSelect.value === "true") {
        airExchangeGroup.style.display = "block";
        airExchangeInput.required = true;
    } else {
        airExchangeGroup.style.display = "none";
        airExchangeInput.required = false;
        airExchangeInput.value = ""; 
    }
}

function toggleWindowArea() {
    const windowSelect = document.getElementById("hasLargeWindow");
    const windowAreaGroup = document.getElementById("windowAreaGroup");
    const windowAreaInput = document.getElementById("windowArea");

    if (windowSelect.value === "true") {
        windowAreaGroup.style.display = "block";
        windowAreaInput.required = true;
    } else {
        windowAreaGroup.style.display = "none";
        windowAreaInput.required = false;
        windowAreaInput.value = ""; 
    }
}

/**
 * 🔍 Валидация формы перед отправкой.
 */
function validateForm() {
    let errors = [];
    const errorContainer = document.getElementById("btuValidationErrors");
    errorContainer.innerHTML = ""; 

    function checkField(value, min, max, fieldName) {
        if (isNaN(value) || value < min || value > max) {
            errors.push(`${fieldName} must be between ${min} and ${max}.`);
        }
    }

    function checkRequired(value, fieldName) {
        if (value === null || value === undefined || value === "") {
            errors.push(`${fieldName} is required.`);
        }
    }

    const fields = [
        { id: "roomSize", min: 1, max: 500, name: "Room Size" },
        { id: "ceilingHeight", min: 2, max: 10, name: "Ceiling Height" },
        { id: "peopleCount", min: 1, max: 100, name: "People Count" },
        { id: "numberOfComputers", min: 0, max: 50, name: "Number of Computers" },
        { id: "numberOfTVs", min: 0, max: 50, name: "Number of TVs" },
        { id: "otherAppliancesKWattage", min: 0, max: 20, name: "Other Appliances Wattage" }
    ];

    fields.forEach(field => checkField(parseFloat(document.getElementById(field.id)?.value), field.min, field.max, field.name));
    checkRequired(document.getElementById("sizeUnit")?.value, "Size Unit");
    checkRequired(document.getElementById("heightUnit")?.value, "Height Unit");
    checkRequired(document.getElementById("sunExposure")?.value, "Sun Exposure");

    if (document.getElementById("hasVentilation")?.checked) {
        checkField(parseFloat(document.getElementById("airExchangeRate")?.value), 0.5, 3.0, "Air Exchange Rate");
    }

    if (document.getElementById("hasLargeWindow")?.checked) {
        checkField(parseFloat(document.getElementById("windowArea")?.value), 0, 100, "Window Area");
    }

    if (errors.length > 0) {
        console.error("❌ Ошибки валидации:", errors);
        errorContainer.innerHTML = `<span style="color: red;">${errors.join("<br>")}</span>`;
        return false;
    }

    return true;
}

function hideBtuHint() {
    const hint = document.getElementById("btuHint");
    if (hint) {
        hint.classList.add("hidden");
    }
}

/**
 * 📏 Выполняет расчет BTU.
 */
async function calculateBTU() {
    if (!validateForm()) return;

    const requestData = {
        room_size: parseFloat(document.getElementById("roomSize")?.value) || 0,
        size_unit: document.getElementById("sizeUnit")?.value || "square meters",
        ceiling_height: parseFloat(document.getElementById("ceilingHeight")?.value) || 0,
        height_unit: document.getElementById("heightUnit")?.value || "meters",
        sun_exposure: document.getElementById("sunExposure")?.value || "medium",
        people_count: parseInt(document.getElementById("peopleCount")?.value) || 0,
        number_of_computers: parseInt(document.getElementById("numberOfComputers")?.value) || 0,
        number_of_tvs: parseInt(document.getElementById("numberOfTVs")?.value) || 0,
        other_appliances_kwattage: parseFloat(document.getElementById("otherAppliancesKWattage")?.value) || 0,
        has_ventilation: document.getElementById("hasVentilation")?.value === "true",
        guaranteed_20_degrees: document.getElementById("guaranteed20Degrees")?.value === "true",
        is_top_floor: document.getElementById("isTopFloor")?.value === "true",
        has_large_window: document.getElementById("hasLargeWindow")?.value === "true"
    };

    console.log("📤 Исходные данные перед обработкой:", requestData);

    if (requestData.has_ventilation) {
        const airExchangeRate = parseFloat(document.getElementById("airExchangeRate")?.value);
        if (!isNaN(airExchangeRate) && airExchangeRate >= 0.5 && airExchangeRate <= 3) {
            requestData.air_exchange_rate = airExchangeRate;
        } else {
            alert("Ошибка: Некорректная кратность воздухообмена (должна быть от 0.5 до 3)");
            return;
        }
    } else {
        requestData.air_exchange_rate = null;  
    }

    if (requestData.has_large_window) {
        const windowArea = parseFloat(document.getElementById("windowArea")?.value);
        if (!isNaN(windowArea) && windowArea >= 0 && windowArea <= 100) {
            requestData.window_area = windowArea;
        } else {
            alert("Ошибка: Некорректная площадь окон (должна быть от 0 до 100 м²)");
            return;
        }
    } else {
        requestData.window_area = null;  
    }

    console.log("📤 Отправляем JSON:", requestData);

    try {
        const response = await fetch("/BTU/Calculate", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(requestData)
        });

        if (!response.ok) {
            const errorMessage = await response.text();
            throw new Error(`Ошибка расчета BTU: ${response.status} - ${errorMessage}`);
        }

        const data = await response.json();
        console.log("✅ Ответ сервера:", data);

        const warningMessage = document.getElementById("btuWarning");
        if (data.calculated_power_btu >= 300000) {
            warningMessage.innerHTML = `<span style="color: orange;text-align:center;">⚠️ Внимание: Рассчитанный BTU превышает 300000 и был ограничен.</span>`;
        } else {
            warningMessage.innerHTML = "";
        }

        document.getElementById("btuResults").style.display = "block";
        document.getElementById("calculatedBTU").innerText = data.calculated_power_btu;
        document.getElementById("calculatedPowerKW").innerText = data.calculated_power_kw;
        document.getElementById("recommendedBTULower").innerText = data.recommended_range_btu.lower;
        document.getElementById("recommendedBTUUpper").innerText = data.recommended_range_btu.upper;
        document.getElementById("recommendedKWLower").innerText = data.recommended_range_kw.lower;
        document.getElementById("recommendedKWUpper").innerText = data.recommended_range_kw.upper;

        const findBtn = document.querySelector(".btu-find-btn");
        if (findBtn) {
            const btuMin = data.recommended_range_btu?.lower;
            const btuMax = data.recommended_range_btu?.upper;

            if (btuMin !== undefined && btuMax !== undefined) {
                findBtn.dataset.btuMin = btuMin;
                findBtn.dataset.btuMax = btuMax;
                findBtn.disabled = false;
            } else {
                console.warn("⚠️ Сервер не вернул recommended_range_btu! Кнопка Find заблокирована.");
                findBtn.dataset.btuMin = "";
                findBtn.dataset.btuMax = "";
                findBtn.disabled = true;
            }
        }

    } catch (error) {
        console.error("❌ Ошибка:", error);
        const resultElement = document.getElementById("btuValidationErrors");
        if (resultElement) {
            resultElement.innerHTML = `<span style="color: red;">Ошибка расчета BTU! ${error.message}</span>`;
        }
    }
}

/**
 * 🔎 Выполняет поиск кондиционеров с учетом расширенного поиска.
 */
async function findConditioners() {
    const btn = document.querySelector(".btu-find-btn");
    const messageContainer = document.getElementById("conditionersMessage");
    const table = document.getElementById("conditionersTable");
    const rangeMessageContainer = document.getElementById("rangeMessageContainer");

    if (!btn) {
        console.error("❌ Ошибка: кнопка `.btu-find-btn` не найдена!");
        return;
    }

    if (!btn.dataset.btuMin || !btn.dataset.btuMax) {
        console.warn("⚠️ Сначала выполните расчет BTU!");
        messageContainer.style.display = "block";
        messageContainer.innerHTML = "⚠️ Сначала выполните расчет BTU!";
        table.style.display = "none";
        return;
    }

    const btuMin = parseInt(btn.dataset.btuMin, 10);
    const btuMax = parseInt(btn.dataset.btuMax, 10);

    if (isNaN(btuMin) || isNaN(btuMax) || btuMin <= 0 || btuMax <= 0) {
        console.warn("⚠️ Некорректные значения BTU!", { btuMin, btuMax });
        messageContainer.style.display = "block";
        messageContainer.innerHTML = "❌ Ошибка: некорректные значения BTU!";
        table.style.display = "none";
        return;
    }

    console.log("📤 Запрос на кондиционеры в диапазоне:", { btuMin, btuMax });

    try {
        let response = await fetch(`/BTU/products/range?btu_min=${btuMin}&btu_max=${btuMax}`);

        if (response.status === 404) {
            console.warn("⚠️ Кондиционеры в заданном диапазоне не найдены. Пробуем экстремальные значения...");
        } else if (!response.ok) {
            throw new Error(`Ошибка запроса: ${response.status}`);
        }

        if (response.ok) {
            const data = await response.json();
            if (data.length > 0) {
                updateConditionersTable(data);
                rangeMessageContainer.style.display = "none"; 
                return;
            } else {
                console.warn("⚠️ Кондиционеры в заданном диапазоне не найдены.");
            }
        }

        response = await fetch("/BTU/products/extremes");
        if (!response.ok) throw new Error("Ошибка получения экстремальных BTU");

        const extremeResponse = await response.json();
        console.log("📊 Полученные экстремальные BTU:", extremeResponse);

        const extremeMin = extremeResponse.btu_min;
        const extremeMax = extremeResponse.btu_max;
        const extremeProducts = extremeResponse.products || [];

        let filteredProducts = [];
        let rangeMessage = "";

        if (btuMax < extremeMin) {
            console.log("🔽 Выбираем кондиционеры с минимальным BTU:", extremeMin);
            filteredProducts = extremeProducts.filter(p => p.btu === extremeMin);
            rangeMessage = `⚠️ Показаны кондиционеры с минимальными значениями BTU (${extremeMin} BTU).`;
        } else if (btuMin > extremeMax) {
            console.log("🔼 Выбираем кондиционеры с максимальным BTU:", extremeMax);
            filteredProducts = extremeProducts.filter(p => p.btu === extremeMax);
            rangeMessage = `⚠️ Показаны кондиционеры с максимальными значениями BTU (${extremeMax} BTU).`;
        } else {
            console.log("🔄 Выбираем кондиционеры в диапазоне BTU:", btuMin, btuMax);
            filteredProducts = extremeProducts.filter(p => p.btu >= btuMin && p.btu <= btuMax);
            rangeMessage = `🔄 Показаны кондиционеры с BTU в диапазоне от ${btuMin} до ${btuMax}.`;
        }

        if (filteredProducts.length > 0) {
            updateConditionersTable(filteredProducts);
            rangeMessageContainer.style.display = "block";
            rangeMessageContainer.innerHTML = rangeMessage;
        } else {
            console.warn("⚠️ Нет кондиционеров, соответствующих экстремальным значениям.");
            messageContainer.style.display = "block";
            messageContainer.innerHTML = "❌ Нет доступных кондиционеров.";
            table.style.display = "none";
        }

    } catch (error) {
        console.error("❌ Ошибка загрузки данных:", error);
        messageContainer.style.display = "block";
        messageContainer.innerHTML = `❌ Ошибка загрузки данных: ${error.message}`;
        table.style.display = "none";
    }
}


/**
 * 🔄 Настройка сортировки таблицы кондиционеров
 */
function setupTableSorting() {
    const table = document.getElementById("conditionersTable");
    const headers = table.querySelectorAll("thead td");

    headers.forEach((header, index) => {
        const isSortable = ["Name", "Store", "Price", "BTU", "Service Area"].includes(header.textContent.trim());

        if (isSortable) {
            header.style.cursor = "pointer";
            header.innerHTML += ` <span class="sort-icon">⬍</span>`; 

            header.addEventListener("click", () => {
                sortTableByColumn(index, header);
            });
        }
    });
}

/**
 * 🔄 Сортирует таблицу кондиционеров по колонке
 * @param {number} columnIndex - Индекс столбца
 * @param {HTMLElement} header - Заголовок столбца
 */
function sortTableByColumn(columnIndex, header) {
    const table = document.getElementById("conditionersTable");
    const tbody = table.querySelector("tbody");
    const rows = Array.from(tbody.querySelectorAll("tr"));

    let sortOrder = header.dataset.sortOrder === "asc" ? "desc" : "asc";
    if (header.dataset.sortOrder === "desc") sortOrder = ""; 

    document.querySelectorAll(".sort-icon").forEach(icon => {
        icon.textContent = "⬍";
        icon.classList.remove("sort-asc", "sort-desc", "sort-reset");
        icon.classList.add("sort-reset");
    });

    document.querySelectorAll("thead td").forEach(td => td.classList.remove("sorted-column"));

    header.dataset.sortOrder = sortOrder;
    header.classList.add("sorted-column");

    const icon = header.querySelector(".sort-icon");

    if (sortOrder === "asc") {
        icon.textContent = "⬆️";
        icon.classList.add("sort-asc");
        icon.classList.remove("sort-desc", "sort-reset");
    } else if (sortOrder === "desc") {
        icon.textContent = "⬇️";
        icon.classList.add("sort-desc");
        icon.classList.remove("sort-asc", "sort-reset");
    } else {
        icon.textContent = "⬍";
        icon.classList.add("sort-reset");
        icon.classList.remove("sort-asc", "sort-desc");
    }

    if (!sortOrder) {
        updateConditionersTable(originalProducts);
        return;
    }

    const isCyrillic = text => /^[\u0400-\u04FF]/.test(text);

    const cleanString = str =>
        str.toLowerCase()
            .replace(/[^\w\sа-яёА-ЯЁ\d]/g, "") 
            .replace(/\s+/g, " ")
            .trim();

    const compareValues = (a, b) => {
        let valA = cleanString(a.children[columnIndex]?.textContent.trim() || "");
        let valB = cleanString(b.children[columnIndex]?.textContent.trim() || "");

        return sortOrder === "asc"
            ? valA.localeCompare(valB, ["ru", "en"], { sensitivity: "base", ignorePunctuation: true })
            : valB.localeCompare(valA, ["ru", "en"], { sensitivity: "base", ignorePunctuation: true });
    };

    const compareNumbers = (a, b) => {
        let valA = a.children[columnIndex]?.textContent.replace(/[^\d.]/g, "") || "0";
        let valB = b.children[columnIndex]?.textContent.replace(/[^\d.]/g, "") || "0";

        valA = parseFloat(valA);
        valB = parseFloat(valB);

        return sortOrder === "asc" ? valA - valB : valB - valA;
    };

    let isNumericColumn = columnIndex === 1 || columnIndex === 2 || columnIndex === 3; 
    let sortedRows;

    if (isNumericColumn) {
        sortedRows = rows.sort(compareNumbers);
    } else {
        let russianRows = [];
        let englishRows = [];

        rows.forEach(row => {
            let cellText = row.children[columnIndex]?.textContent.trim() || "";
            if (isCyrillic(cellText)) {
                russianRows.push(row);
            } else {
                englishRows.push(row);
            }
        });

        russianRows.sort(compareValues);
        englishRows.sort(compareValues);

        sortedRows = russianRows.concat(englishRows);
    }

    tbody.innerHTML = "";
    sortedRows.forEach(row => tbody.appendChild(row));
}

let originalProducts = [];

/**
 * 🔄 Обновляет таблицу кондиционеров и сохраняет оригинальный порядок
 */
function updateConditionersTable(products) {
    const table = document.getElementById("conditionersTable");
    const tableBody = document.getElementById("conditionersTableBody");
    const messageContainer = document.getElementById("conditionersMessage");

    if (!products || products.length === 0) {
        messageContainer.style.display = "block";
        messageContainer.innerHTML = "❌ Нет доступных кондиционеров";
        table.style.display = "none";
        return;
    }

    messageContainer.style.display = "none";
    table.style.display = "table";

    originalProducts = [...products]; 

    tableBody.innerHTML = products.map(product => `
        <tr>
            <td><a href="${product.url}" target="_blank">${product.name || "—"}</a></td>
            <td>${product.price ? `${product.price} ${product.currency}` : "—"}</td>
            <td>${product.btu ? `${product.btu} BTU` : "—"}</td>
            <td>${product.service_area ? `${product.service_area} м²` : "—"}</td>
            <td>${product.store || "—"}</td>
        </tr>`).join("");
}