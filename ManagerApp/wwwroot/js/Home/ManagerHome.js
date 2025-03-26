document.addEventListener("DOMContentLoaded", function () {
    let lastOrdersHash = "";

    async function loadClients() {
        let customersTable = document.querySelector("#customersTable tbody");
        if (!customersTable) return;

        try {
            let response = await fetch("/get-clients");
            if (!response.ok) throw new Error("Failed to fetch clients.");
            let data = await response.json();

            customersTable.innerHTML = data.length === 0
                ? "<tr><td colspan='2'>No customers found</td></tr>"
                : data.map(client => `
                    <tr>
                        <td width="60px">
                            <div class="imgBx">
                                <img src="${client.avatarUrl ? `/imgs/Home/${client.avatarUrl}` : "/imgs/Home/customer01.jpg"}"
                                    onerror="this.onerror=null;this.src='/imgs/Home/customer01.jpg';">
                            </div>
                        </td>
                        <td>
                            <h4>${client.firstName} ${client.lastName} <br> 
                                <span>${client.address || "No address"}</span>
                            </h4>
                        </td>
                    </tr>`).join("");
        } catch {
            customersTable.innerHTML = "<tr><td colspan='2'>Failed to load customers</td></tr>";
        }
    }

    async function loadRecentOrders() {
        let ordersContainer = document.querySelector("#ordersContainer");
        if (!ordersContainer) return;

        try {
            let response = await fetch(`/Orders/GetOrders?recent=true`);
            if (!response.ok) throw new Error(`Failed to load recent orders. Status: ${response.status}`);
            let data = await response.json();

            let orders = Array.isArray(data.orders) ? data.orders : (Array.isArray(data) ? data : []);
            orders.sort((a, b) => new Date(b.creationOrderDate) - new Date(a.creationOrderDate));
            let recentOrders = orders.slice(0, 10);

            let newOrdersHash = JSON.stringify(recentOrders);
            if (newOrdersHash !== lastOrdersHash) {
                lastOrdersHash = newOrdersHash;
                updateActiveOrders(recentOrders);
                renderRecentOrders(recentOrders);
            }
        } catch (error) {
            console.error("❌ Ошибка загрузки последних заказов:", error);
        }
    }

    function updateActiveOrders(orders) {
        document.querySelector(".cardBox .card:nth-child(1) .numbers").textContent = orders.length;

        let totalEarnings = orders
            .filter(order => order.paymentStatus === "Paid")
            .reduce((sum, order) => sum + (order.totalCost || 0), 0);

        document.querySelector(".cardBox .card:nth-child(4) .numbers").textContent = `${totalEarnings.toFixed(2)} MDL`;
    }

    function renderRecentOrders(orders) {
        let ordersContainer = document.querySelector("#ordersContainer");
        if (orders.length === 0) {
            ordersContainer.innerHTML = `
            <div class="no-orders-message">
                <div class="no-orders-icon">&#128721;</div>
                <p>No recent orders available.</p>
            </div>`;
            return;
        }

        ordersContainer.innerHTML = `
        <table id="recentOrdersTable">
            <thead>
                <tr>
                    <td>Order Type</td>
                    <td>Price (MDL)</td>
                    <td>Payment</td>
                    <td>Status</td>
                </tr>
            </thead>
            <tbody id="ordersBody"></tbody>
        </table>`;

        const ordersTable = document.querySelector("#ordersBody");

        orders.forEach(order => {
            let statusClass = mapStatusToClass(order.fulfillmentStatus);
            let orderIcon = getOrderIcon(order.orderType);

            const row = `
            <tr>
                <td>
                    <div class="order-type">
                        <span class="order-icon">${orderIcon}</span>
                        <span class="order-name">${order.orderType || "Unknown"}</span>
                    </div>
                </td> 
                <td>
                    <div class="order-price">
                        <span class="price-value">${order.totalCost ? order.totalCost.toFixed(2) : "0.00"}</span>
                        <span class="currency">MDL</span>
                    </div>
                </td>   
                <td>
                    <div class="payment-status">${order.paymentStatus || "N/A"}</div>
                </td>
                <td>
                    <span class="status ${statusClass}">${order.fulfillmentStatus || "Unknown"}</span>
                </td>
            </tr>`;
            ordersTable.insertAdjacentHTML("beforeend", row);
        });
    }

    function mapStatusToClass(status) {
        switch (status) {
            case "Completed": return "completed";
            case "Processing":
            case "InProgress": return "processing";
            case "Cancelled":
            case "Cancel": return "cancel";
            case "New":
            case "Pending": return "inProgress";
            default: return "unknown";
        }
    }

    function getOrderIcon(orderType) {
        switch (orderType) {
            case "Installation": return "🛠️";
            case "Maintenance": return "🔧";
            case "Repair": return "⚙️";
            case "Delivery": return "🚚";
            default: return "📦";
        }
    }

    async function fetchAvailableTechnicianCount() {
        try {
            const response = await fetch("/technicians/available-today", {
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });

            if (!response.ok) throw new Error("Failed to fetch technician data");

            const technicianList = await response.json();

            console.log("👷 Доступные техники:", technicianList);

            const techCount = Array.isArray(technicianList) ? technicianList.length : 0;

            const techCounter = document.getElementById("availableTechnicians");
            if (techCounter) {
                techCounter.textContent = techCount;
            }
        } catch (error) {
            console.error("❌ Ошибка при получении списка техников:", error);
        }
    }

    async function fetchTotalEquipmentCount() {
        try {
            const response = await fetch("/equipment/all-warehouses", {
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });

            if (!response.ok) throw new Error("Failed to fetch equipment data");

            const equipmentList = await response.json();

            console.log("📦 Получен список оборудования:", equipmentList);

            let total = 0;
            for (const item of equipmentList) {
                total += item.totalQuantity || 0;
            }

            const equipmentCounter = document.getElementById("equipmentCount");
            if (equipmentCounter) {
                equipmentCounter.textContent = total;
            }
        } catch (error) {
            console.error("❌ Ошибка при получении оборудования:", error);
        }
    }

    fetchAvailableTechnicianCount();
    fetchTotalEquipmentCount();
    loadRecentOrders();
    loadClients();


    setInterval(() => {
        console.log("🔄 Проверка новых заказов...");
        loadRecentOrders();
    }, 10000);
});
