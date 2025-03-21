document.addEventListener("DOMContentLoaded", function () {
    let activeDropdownMenu = null;
    let currentPage = 1;
    let ordersPerPage = 10;
    let lastOrdersHash = "";
    let currentStatusFilter = ""; 

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

            const isActive = dropdown.classList.contains("active");

            if (isActive) {
                dropdown.classList.remove("active");
                dropdownMenu.style.display = "none";
                activeDropdownMenu = null;
            } else {
                dropdown.classList.add("active");
                dropdownMenu.style.display = "block";
                activeDropdownMenu = dropdownMenu;
            }
        }

        if (e.target.classList.contains("order-number")) {
            const orderId = e.target.dataset.id;
            if (orderId) {
                window.location.href = `/Orders/Details/${orderId}`;
            }
        }

        if (e.target.classList.contains("view-order-link")) {
            e.preventDefault();
            const orderId = e.target.dataset.id;
            if (orderId) {
                window.location.href = `/Orders/Details/${orderId}`;
            }
        }

        if (e.target.classList.contains("edit-order-link")) {
            e.preventDefault();
            const orderId = e.target.dataset.id;
            if (orderId) {
                window.location.href = `/Orders/Edit/${orderId}`;
            }
        }

        if (e.target.classList.contains("filter-btn")) {
            document.querySelectorAll(".filter-btn").forEach(btn => btn.classList.remove("active"));
            e.target.classList.add("active");

            currentStatusFilter = e.target.dataset.status || ""; 

            currentPage = 1; 
            loadOrders(currentPage, ordersPerPage, false, currentStatusFilter);
        }
    });

    document.getElementById("ordersPerPage").addEventListener("change", function () {
        ordersPerPage = parseInt(this.value);
        currentPage = 1;
        loadOrders(currentPage, ordersPerPage, false, currentStatusFilter);
    });

    document.querySelector(".prev-btn").addEventListener("click", function () {
        if (currentPage > 1) {
            currentPage--;
            loadOrders(currentPage, ordersPerPage, false, currentStatusFilter);
        }
    });

    document.querySelector(".next-btn").addEventListener("click", function () {
        currentPage++;
        loadOrders(currentPage, ordersPerPage, false, currentStatusFilter);
    });


    document.getElementById("searchOrders").addEventListener("input", function () {
        searchQuery = this.value.trim();
        currentPage = 1;
        loadOrders(currentPage, ordersPerPage, false, currentStatusFilter, searchQuery);
    });



    async function loadOrders(page = 1, size = 10, isAutoUpdate = false, statusFilter = "", searchQuery = "") {
        try {
            let url = `/Orders/GetOrders?page=${page}&size=${size}`;
            if (statusFilter) url += `&status=${statusFilter}`;
            if (searchQuery) url += `&query=${encodeURIComponent(searchQuery)}`;

            let response = await fetch(url, {
                method: 'GET',
                headers: { 'Content-Type': 'application/json' }
            });

            if (!response.ok) throw new Error(`Failed to load orders. Status: ${response.status}`);

            let data = await response.json();
            let orders = Array.isArray(data.orders) ? data.orders : (Array.isArray(data) ? data : []);

            if (!Array.isArray(orders)) {
                console.error("❌ Некорректный формат данных:", data);
                showNoOrdersMessage();
                return;
            }

            let newOrdersHash = JSON.stringify(orders);
            if (isAutoUpdate && newOrdersHash === lastOrdersHash) {
                console.log("✅ Заказы не изменились. Пропускаем обновление.");
                return;
            }

            lastOrdersHash = newOrdersHash;

            updateOrdersCount(data.totalCount || orders.length);
            transitionOrders(orders);
            updatePagination(data.totalCount || orders.length, page, size);
        } catch (error) {
            console.error("❌ Ошибка загрузки заявок:", error);
            showNoOrdersMessage();
        }
    }

    function updateOrdersCount(totalOrders) {
        document.querySelector(".orders-count").textContent = totalOrders;
    }

    function transitionOrders(orders) {
        const tableBody = document.querySelector(".orders-table tbody");
        const noOrdersContainer = document.querySelector(".no-orders-container");

        if (orders.length === 0) {
            showNoOrdersMessage();
            return;
        }

        if (noOrdersContainer) {
            noOrdersContainer.classList.add("fade-out");
            setTimeout(() => {
                renderOrders(orders);
            }, 500);
        } else {
            renderOrders(orders);
        }
    }

    function renderOrders(orders) {
        const tableBody = document.querySelector(".orders-table tbody");
        tableBody.innerHTML = "";

        orders.forEach((order, index) => {
            const rowNumber = (currentPage - 1) * ordersPerPage + index + 1;

            const row = document.createElement("tr");
            row.classList.add("fade-in");

            row.innerHTML = `
                <td><span class="order-number" data-id="${order.id}">#${rowNumber}</span></td>
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
                            <a href="#" class="view-order-link" data-id="${order.id}">View Order</a>
                            <a href="#" class="edit-order-link" data-id="${order.id}">Edit Order</a>
                        </div>
                    </div>
                </td>
            `;

            tableBody.appendChild(row);
        });
    }

    function updatePagination(totalOrders, currentPage, ordersPerPage) {
        const totalPages = Math.ceil(totalOrders / ordersPerPage);

        document.getElementById("totalOrders").textContent = totalOrders;
        document.querySelector(".prev-btn").disabled = currentPage === 1;
        document.querySelector(".next-btn").disabled = currentPage >= totalPages;
    }

    function showNoOrdersMessage() {
        const tableBody = document.querySelector(".orders-table tbody");
        tableBody.innerHTML = `
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
        console.log("🔄 Проверка новых заказов...");
        loadOrders(currentPage, ordersPerPage, true, currentStatusFilter);
    }, 5000);

    loadOrders(currentPage, ordersPerPage, false, currentStatusFilter);
});
