document.addEventListener("DOMContentLoaded", function () {
    let activeDropdownMenu = null;
    let currentPage = 1;
    let ordersPerPage = 10;
    let currentFulfillmentStatusFilter = "";
    let currentPaymentStatusFilter = "";
    let searchQuery = "";
    let lastOrdersHash = "";
    let allOrders = [];

    document.addEventListener("click", function (e) {
        if (activeDropdownMenu && !e.target.closest(".dropdown")) {
            activeDropdownMenu.style.display = "none";
            activeDropdownMenu.closest(".dropdown").classList.remove("active");
            activeDropdownMenu = null;
        }

        if (e.target.closest(".dropdown-toggle")) {
            e.preventDefault();
            const dropdown = e.target.closest(".dropdown");
            const dropdownMenu = dropdown.querySelector(".dropdown-menu");
            if (!dropdownMenu) return;

            if (activeDropdownMenu && activeDropdownMenu !== dropdownMenu) {
                activeDropdownMenu.style.display = "none";
                activeDropdownMenu.closest(".dropdown").classList.remove("active");
            }

            dropdown.classList.toggle("active");
            dropdownMenu.style.display = dropdown.classList.contains("active") ? "block" : "none";
            activeDropdownMenu = dropdownMenu;
        }

        if (e.target.classList.contains("order-number")) {
            const orderId = e.target.dataset.id;
            if (orderId) window.location.href = `/Orders/Details/${orderId}`;
        }

        if (e.target.classList.contains("filter-btn")) {
            document.querySelectorAll(".filter-btn").forEach(btn => btn.classList.remove("active"));
            e.target.classList.add("active");

            const filterType = e.target.dataset.filterType;
            const filterValue = e.target.dataset.status || "";

            if (filterType === "all") {
                currentFulfillmentStatusFilter = "";
                currentPaymentStatusFilter = "";
            } else if (filterType === "fulfillmentStatus") {
                currentFulfillmentStatusFilter = filterValue;
                currentPaymentStatusFilter = "";
            } else if (filterType === "paymentStatus") {
                currentPaymentStatusFilter = filterValue;
                currentFulfillmentStatusFilter = "";
            }

            console.log(`📌 Установлен фильтр: выполнение = ${currentFulfillmentStatusFilter}, оплата = ${currentPaymentStatusFilter}`);
            currentPage = 1;
            applyFiltersAndRenderOrders();
        }
    });

    document.getElementById("searchOrders").addEventListener("input", function () {
        searchQuery = this.value.trim().toLowerCase();
        currentPage = 1;
        applyFiltersAndRenderOrders();
    });

    async function loadOrders() {
        try {
            let url = `/Orders/GetOrders`;
            console.log("🔗 Запрос к API:", url);

            let response = await fetch(url, { method: 'GET', headers: { 'Content-Type': 'application/json' } });
            if (!response.ok) throw new Error(`Ошибка загрузки заказов. Статус: ${response.status}`);

            let data = await response.json();
            allOrders = Array.isArray(data) ? data : data.orders;

            if (!Array.isArray(allOrders)) {
                console.warn("⚠ Некорректный ответ от сервера. Ожидался массив заказов, получено:", data);
                showNoOrdersMessage();
                return;
            }

            let newOrdersHash = JSON.stringify(allOrders);
            if (newOrdersHash === lastOrdersHash) {
                console.log("✅ Данные не изменились. Пропускаем обновление.");
                return;
            }

            lastOrdersHash = newOrdersHash;
            applyFiltersAndRenderOrders();
        } catch (error) {
            console.error("❌ Ошибка загрузки заказов:", error);
            showNoOrdersMessage();
        }
    }

    function applyFiltersAndRenderOrders() {
        let filteredOrders = allOrders;

        if (currentFulfillmentStatusFilter) {
            filteredOrders = filteredOrders.filter(order => order.fulfillmentStatus.toLowerCase() === currentFulfillmentStatusFilter.toLowerCase());
        }

        if (currentPaymentStatusFilter) {
            filteredOrders = filteredOrders.filter(order => order.paymentStatus.toLowerCase() === currentPaymentStatusFilter.toLowerCase());
        }

        if (searchQuery) {
            filteredOrders = filteredOrders.filter(order =>
                order.id.toLowerCase().includes(searchQuery) ||
                order.clientName?.toLowerCase().includes(searchQuery) ||
                order.paymentMethod?.toLowerCase().includes(searchQuery) ||
                new Date(order.installationDate).toLocaleString().toLowerCase().includes(searchQuery)
            );
        }

        if (filteredOrders.length === 0) {
            showNoOrdersMessage();
            return;
        }

        updateOrdersCount(filteredOrders.length);
        renderOrders(filteredOrders.slice((currentPage - 1) * ordersPerPage, currentPage * ordersPerPage));
        updatePagination(filteredOrders.length);
    }

    function updateOrdersCount(totalOrders) {
        document.querySelector(".orders-count").textContent = totalOrders;
    }

    function renderOrders(orders) {
        console.log("🖌 Отрисовка заказов...");
        const tableBody = document.querySelector(".orders-table tbody");
        tableBody.innerHTML = "";

        orders.forEach((order, index) => {
            const rowNumber = (currentPage - 1) * ordersPerPage + index + 1;
            const orderIdShort = order.id.substring(0, 8);

            const row = document.createElement("tr");
            row.classList.add("fade-in");

            row.innerHTML = `
                <td><span class="order-number link" data-id="${order.id}">#${orderIdShort}</span></td>
                <td>${new Date(order.installationDate).toLocaleString()}</td>
                <td>${order.clientName || "Unknown"}</td>
                <td><span class="status ${order.paymentStatus.toLowerCase()}">${order.paymentStatus}</span></td>
                <td><span class="status ${order.fulfillmentStatus.toLowerCase()}">${order.fulfillmentStatus}</span></td>
                <td>${order.paymentMethod}</td>
                <td>
                    <div class="dropdown">
                        <button class="action-btn dropdown-toggle">
                            Actions <span class="arrow-down">&#9662;</span>
                        </button>
                        <div class="dropdown-menu">
                            <a href="/Orders/Details/${order.id}" class="view-order-link">👁 View Order</a>
                            <a href="/Orders/Edit/${order.id}" class="edit-order-link">✏ Edit Order</a>
                        </div>
                    </div>
                </td>
            `;

            tableBody.appendChild(row);
        });

        console.log("✅ Отрисовано", orders.length, "заказов.");
    }

    function updatePagination(totalOrders) {
        const totalPages = Math.ceil(totalOrders / ordersPerPage);
        document.getElementById("totalOrders").textContent = totalOrders;
        document.querySelector(".prev-btn").disabled = currentPage === 1;
        document.querySelector(".next-btn").disabled = currentPage >= totalPages;
    }

    function showNoOrdersMessage() {
        document.querySelector(".orders-table tbody").innerHTML = `
            <tr>
                <td colspan="7" class="no-orders-container">
                    <div class="no-orders-content">
                        <div class="no-orders-icon">🚫</div>
                        <p class="no-orders-text">No orders found</p>
                    </div>
                </td>
            </tr>`;
    }

    setInterval(() => {
        console.log("🔄 Автообновление заказов...");
        loadOrders();
    }, 5000);

    loadOrders();
});
