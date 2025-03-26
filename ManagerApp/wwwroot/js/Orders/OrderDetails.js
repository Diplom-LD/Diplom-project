document.addEventListener("DOMContentLoaded", async function () {

    console.log("✅ DOMContentLoaded сработал");

    window.addEventListener("error", (e) => {
        console.error("💥 JS error:", e.message, e.filename, e.lineno);
    });

    window.addEventListener("unhandledrejection", (e) => {
        console.error("💥 Unhandled Promise rejection:", e.reason);
    });


    let technicianSocket = null;
    let isTrackingConnected = false;
    let currentPopup = null;

    /* Взаимодействие с картой */
    const mapElement = document.getElementById("map");
    console.log("🧭 mapElement:", mapElement);
    if (!mapElement) return;


    const routesData = mapElement.dataset.routes;
    if (!routesData) return;

    let initialRoutes = [];

    try {
        initialRoutes = JSON.parse(routesData);
        if (!initialRoutes.length) return; 
        console.log("✅ Routes parsed", initialRoutes);
    } catch (err) {
        console.error("❌ Ошибка парсинга routesData:", err, routesData);
        return;
    }

    /* Popup technician on map */
    function createMarkerWithPopup({ lngLat, color, html }) {
        const popup = new maplibregl.Popup({ offset: 25 }).setHTML(html);
        return new maplibregl.Marker({ color }).setLngLat(lngLat).setPopup(popup).addTo(map);
    }


    let apiKey = null;
    try {
        const response = await fetch("/maps/api-key");
        const data = await response.json();
        apiKey = data.apiKey;
    } catch (error) {
        console.error("❌ Ошибка получения API-ключа:", error);
        return;
    }

    const map = new maplibregl.Map({
        container: "map",
        style: `https://api.maptiler.com/maps/streets/style.json?key=${apiKey}`,
        center: [28.85, 47.0],
        zoom: 13
    });

    map.addControl(new maplibregl.NavigationControl());
    map.addControl(new maplibregl.FullscreenControl());

    setTimeout(() => {
        document.querySelector(".maplibregl-ctrl-attrib-button")?.remove();
        document.querySelector(".maplibregl-ctrl-attrib")?.remove();
    }, 1000);

    const routeColors = [
        "#FF8C00", "#33FF57", "#5733FF", "#FF33A8", "#33FFF5", "#FF5733",
        "#F5FF33", "#ADFF2F", "#8A2BE2", "#DC143C", "#00CED1", "#32CD32"
    ];

    let bounds = new maplibregl.LngLatBounds();
    let routeLayers = [];
    let technicianMarkers = [];
    let warehouseMarkers = [];
    let installationMarkers = [];

    const routeLegendList = document.getElementById("route-legend-list");

    function getUniqueRouteColor(index) {
        return routeColors[index % routeColors.length];
    }

    map.on("load", function () {
        initialRoutes.forEach((route, index) => {
            const color = getUniqueRouteColor(index);
            const points = route.routePoints.map(p => [p.longitude, p.latitude]);

            points.forEach(point => bounds.extend(point));

            const routeId = `route-line-${index}`;
            map.addSource(routeId, {
                type: "geojson",
                data: {
                    type: "Feature",
                    properties: {
                        technicianName: route.technicianName,
                        phoneNumber: route.phoneNumber
                    },
                    geometry: {
                        type: "LineString",
                        coordinates: points
                    }
                }
            });

            map.addLayer({
                id: routeId,
                type: "line",
                source: routeId,
                layout: {
                    "line-join": "round",
                    "line-cap": "round"
                },
                paint: {
                    "line-color": color,
                    "line-width": 4
                }
            });

            map.on("click", routeId, function (e) {
                const { technicianName, phoneNumber } = e.features[0].properties;
                const coordinates = e.lngLat;
                showMapPopup(map, coordinates, `
                    <div class="popup-card">
                        <div class="popup-name">👨‍🔧 <strong>${technicianName}</strong></div>
                        <div class="popup-phone">📞 ${phoneNumber}</div>
                    </div>
                `);
            });


            const startPoint = points[0];
            const technicianPopup = new maplibregl.Popup({ offset: 25 }).setHTML(`
                <div class="popup-card">
                    <div class="popup-name">👨‍🔧 <strong>${route.technicianName}</strong></div>
                    <div class="popup-phone">📞 ${route.phoneNumber}</div>
                </div>
            `);

            const technicianMarker = new maplibregl.Marker({ color: "blue" })
                .setLngLat(startPoint)
                .setPopup(technicianPopup)
                .addTo(map);

            technicianMarker.__techId = route.technicianId; 
            technicianMarkers.push(technicianMarker);

            const endPoint = points[points.length - 1];
            const installationPopup = new maplibregl.Popup({ offset: 25 }).setHTML(`
                <div class="popup-card">
                    <div class="popup-name">📍 <strong>Installation Point</strong></div>
                </div>
            `);
            const installationMarker = new maplibregl.Marker({ color: "red" })
                .setLngLat(endPoint)
                .setPopup(installationPopup)
                .addTo(map);
            installationMarkers.push(installationMarker);



            installationMarkers.push(installationMarker);

            const warehousePoint = route.routePoints.find(p => p.isStopPoint);
            if (warehousePoint) {
                const warehousePopup = new maplibregl.Popup({ offset: 25 }).setHTML(`
                    <div class="popup-card">
                        <div class="popup-name">🏭 <strong>Warehouse</strong></div>
                    </div>
                `);
                const warehouseMarker = new maplibregl.Marker({ color: "#8B4513" })
                    .setLngLat([warehousePoint.longitude, warehousePoint.latitude])
                    .setPopup(warehousePopup)
                    .addTo(map);
                warehouseMarkers.push(warehouseMarker);
            }

            const legendItem = document.createElement("li");
            legendItem.innerHTML = `<input type="checkbox" id="${routeId}" name="${routeId}" checked data-route-id="${routeId}"><label for="${routeId}"><span style="color: ${color}; font-weight: bold; margin-left: 3px;">⬤</span> ${route.technicianName}</label>`;
            legendItem.querySelector("input").addEventListener("change", function () {
                const visibility = this.checked ? "visible" : "none";
                map.setLayoutProperty(routeId, "visibility", visibility);
            });
            routeLegendList.appendChild(legendItem);
            routeLayers.push(routeId);
        });

        if (!bounds.isEmpty()) {
            map.fitBounds(bounds, { padding: 50 });
        }
    });

    document.addEventListener("fullscreenchange", () => {
        const legend = document.querySelector(".map-legend");
        const fsElement = document.fullscreenElement;

        if (!legend || !fsElement) return;

        const canvasContainer = fsElement.querySelector(".maplibregl-canvas-container");

        if (canvasContainer && !canvasContainer.contains(legend)) {
            console.log("📦 Перемещаем легенду в fullscreen");
            canvasContainer.appendChild(legend);
        } else {
            const originalContainer = document.querySelector(".map-container");
            if (originalContainer && !originalContainer.contains(legend)) {
                console.log("↩️ Возвращаем легенду обратно");
                originalContainer.appendChild(legend);
            }
        }
    });

    document.addEventListener("change", function (e) {
        if (e.target.id === "toggle-technician") {
            technicianMarkers.forEach(marker => {
                marker.getElement().style.display = e.target.checked ? "block" : "none";
            });
        }
        if (e.target.id === "toggle-warehouse") {
            warehouseMarkers.forEach(marker => {
                marker.getElement().style.display = e.target.checked ? "block" : "none";
            });
        }
        if (e.target.id === "toggle-client") {
            installationMarkers.forEach(marker => {
                marker.getElement().style.display = e.target.checked ? "block" : "none";
            });
        }
        if (e.target.dataset.routeId) {
            const visibility = e.target.checked ? "visible" : "none";
            map.setLayoutProperty(e.target.dataset.routeId, "visibility", visibility);
        }
    });

    /* Сворачивание легенды*/
    const legend = document.querySelector(".map-legend");
    const toggleBtn = document.getElementById("toggle-legend-btn");

    if (legend && toggleBtn) {
        toggleBtn.addEventListener("click", () => {
            legend.classList.toggle("hidden");
            toggleBtn.textContent = legend.classList.contains("hidden") ? "📑" : "✖";
            toggleBtn.title = legend.classList.contains("hidden") ? "Show Legend" : "Hide Legend";
        });
    }

    /* Цвет. эффекты статусов */
    function applyStatusStyle(select) {
        const value = select.value.toLowerCase();
        const allClasses = [
            "status-new",
            "status-inprogress",
            "status-completed",
            "status-cancelled",
            "status-paid",
            "status-unpaid"
        ];

        select.classList.remove(...allClasses);

        if (value) {
            select.classList.add(`status-${value}`);
        }
    }

    document.querySelectorAll(".order-status-dropdown").forEach((select) => {
        applyStatusStyle(select);
        select.addEventListener("change", () => {
            applyStatusStyle(select);
        });
    });

    function initializeStatusDropdowns() {
        document.querySelectorAll(".order-status-dropdown").forEach((select) => {
            applyStatusStyle(select);

            select.removeEventListener("change", handleStatusChange);
            select.addEventListener("change", handleStatusChange);
        });
    }

    function handleStatusChange(e) {
        applyStatusStyle(e.target);
    }

    /* Time line */
    const timelineSteps = document.querySelectorAll(".timeline-step");
    const timeline = document.querySelector(".order-card__timeline");
    const current = timeline?.dataset.current;

    let reachedCurrent = false;
    let lastVisible = null;

    timelineSteps.forEach((stepEl) => {
        const step = stepEl.dataset.step;

        if (!reachedCurrent && step === current) {
            stepEl.classList.add("timeline-step", "current");
            reachedCurrent = true;
            lastVisible = stepEl;
        } else if (!reachedCurrent) {
            stepEl.classList.add("timeline-step", "completed");
            lastVisible = stepEl;
        } else {
            stepEl.classList.add("timeline-step", "upcoming");
        }
    });

    if (lastVisible) {
        lastVisible.classList.add("last-visible");
    }

    // Асинхронное обновление заявки 
    let lastOrderHash = null;

    async function fetchOrderDetails() {
        const orderId = window.location.pathname.split("/").pop();
        try {
            const response = await fetch(`/orders/details/${orderId}`, {
                method: "GET",
                headers: { "X-Requested-With": "XMLHttpRequest" }
            });

            if (!response.ok) throw new Error("Failed to fetch order");

            const html = await response.text();
            const parser = new DOMParser();
            const doc = parser.parseFromString(html, "text/html");

            const newContent = doc.querySelector(".order-details");
            const currentContent = document.querySelector(".order-details");

            const newHash = newContent.innerHTML;

            if (lastOrderHash && lastOrderHash === newHash) return;
            lastOrderHash = newHash;

            updateSection(".order-card--client", newContent);
            updateSection(".order-card--installation", newContent);
            updateSection(".order-card--materials", newContent);
            updateSection(".order-details__statuses", newContent);
            updateSection(".order-details__date", newContent, true);

            initializeStatusDropdowns();
            initializePaymentStatusDropdown();

            updateTimeline(newContent);
            initializeFulfillmentStatusDropdown(); 
        } catch (error) {
            console.error("❌ Error updating order details:", error);
        }
    }

    function updateSection(selector, newDoc, isMultiple = false) {
        const current = document.querySelectorAll(selector);
        const updated = newDoc.querySelectorAll(selector);

        if (!current || !updated || current.length !== updated.length) return;

        current.forEach((el, i) => {
            let newHtml = updated[i].innerHTML;

            if (selector === ".order-card--materials") {
                const tempContainer = document.createElement("div");
                tempContainer.innerHTML = newHtml;

                const materialRows = Array.from(tempContainer.querySelectorAll(".materials-section .item-row"));

                materialRows.sort((a, b) => {
                    const aPrice = parseFloat(a.querySelector(".item-price")?.textContent?.replace(/\D/g, "") || "0");
                    const bPrice = parseFloat(b.querySelector(".item-price")?.textContent?.replace(/\D/g, "") || "0");
                    return aPrice - bPrice;
                });

                const container = tempContainer.querySelector(".materials-section .item-list");
                if (container) {
                    container.innerHTML = "";
                    materialRows.forEach(row => container.appendChild(row));
                    newHtml = tempContainer.innerHTML;
                }
            }

            if (el.innerHTML !== newHtml) {
                el.innerHTML = newHtml;
            }
        });
    }

    function updateTimeline(newDoc) {
        const newTimeline = newDoc.querySelector(".order-card__timeline");
        const currentTimeline = document.querySelector(".order-card__timeline");

        if (!newTimeline || !currentTimeline) return;

        currentTimeline.innerHTML = newTimeline.innerHTML;

        const timelineSteps = currentTimeline.querySelectorAll(".timeline-step");
        const current = currentTimeline.dataset.current;

        let reachedCurrent = false;
        let lastVisible = null;

        timelineSteps.forEach((stepEl) => {
            const step = stepEl.dataset.step;
            stepEl.classList.remove("completed", "current", "upcoming", "last-visible");

            if (!reachedCurrent && step === current) {
                stepEl.classList.add("current");
                reachedCurrent = true;
                lastVisible = stepEl;
            } else if (!reachedCurrent) {
                stepEl.classList.add("completed");
                lastVisible = stepEl;
            } else {
                stepEl.classList.add("upcoming");
            }
        });

        if (lastVisible) {
            lastVisible.classList.add("last-visible");
        }
    }

    function handlePaymentStatusUpdate(select) {
        const orderId = window.location.pathname.split("/").pop();
        const value = select.value;

        const payload = {
            OrderId: orderId,
            Notes: null,
            WorkCost: null,
            ClientName: null,
            ClientPhone: null,
            ClientEmail: null,
            PaymentStatus: value, 
            PaymentMethod: null,
            FulfillmentStatus: null
        };

        fetch(`/manager/orders/update/${orderId}`, {
            method: "PUT",
            headers: {
                "Content-Type": "application/json",
                "X-Requested-With": "XMLHttpRequest"
            },
            body: JSON.stringify(payload)
        })
            .then(response => {
                if (!response.ok) throw new Error("Failed to update payment status.");
                return response.json();
            })
            .then(data => {
                console.log("✅ Payment status updated:", data.message);
                fetchOrderDetails(); 
            })
            .catch(error => {
                console.error("❌ Error updating payment status:", error);
            });
    }

    function initializePaymentStatusDropdown() {
        const dropdown = document.getElementById("payment-status");
        if (!dropdown) return;

        dropdown.removeEventListener("change", handlePaymentStatusChange);
        dropdown.addEventListener("change", handlePaymentStatusChange);
    }

    function handlePaymentStatusChange(e) {
        handlePaymentStatusUpdate(e.target);
    }

    initializePaymentStatusDropdown();

    (function sortInitialMaterials() {
        const materialsCard = document.querySelector(".order-card--materials");
        if (!materialsCard) return;

        const materialRows = Array.from(materialsCard.querySelectorAll(".materials-section .item-row"));

        materialRows.sort((a, b) => {
            const aPrice = parseFloat(a.querySelector(".item-price")?.textContent?.replace(/\D/g, "") || "0");
            const bPrice = parseFloat(b.querySelector(".item-price")?.textContent?.replace(/\D/g, "") || "0");
            return aPrice - bPrice;
        });

        const container = materialsCard.querySelector(".materials-section .item-list");
        if (container) {
            container.innerHTML = "";
            materialRows.forEach(row => container.appendChild(row));
        }
    })();


    /* Обновление fulfillment */
    function handleFulfillmentStatusChange(e) {
        const newStatus = e.target.value;
        const orderId = window.location.pathname.split("/").pop();

        console.log("🔁 Выбран новый FulfillmentStatus:", newStatus);
        console.log("📦 Отправляем запрос на сервер:", {
            orderId,
            newStatus
        });

        const payload = {
            orderId,
            newStatus
        };

        fetch("/manager/orders/update-status", {
            method: "PUT",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(payload)
        })
            .then(response => {
                console.log("📨 Ответ от сервера:", response.status, response.statusText);
                if (!response.ok) throw new Error("Failed to update fulfillment status");
                return response.json();
            })
            .then(data => {
                console.log("✅ Fulfillment status updated:", data.message);

                fetchOrderDetails().then(() => {
                    console.log("🌐 Проверка WebSocket подключения после fetchOrderDetails");
                    console.log("📦 initialRoutes:", initialRoutes);
                    console.log("🌐 isTrackingConnected =", isTrackingConnected);

                    const shouldConnectWs = !isTrackingConnected && initialRoutes.length > 0;

                    if (shouldConnectWs) {
                        console.log("📡 Подключаем WebSocket — есть маршруты и ещё не подключено");
                        connectToTechnicianTracking(orderId, () => {
                            console.log("🔁 WebSocket подключён.");
                        });
                    } else {
                        console.log("📴 WebSocket не требуется или уже подключён");
                    }
                });


            })
            .catch(err => {
                console.error("❌ Ошибка обновления FulfillmentStatus:", err);
            });
    }

    function initializeFulfillmentStatusDropdown() {
        const dropdown = document.getElementById("fulfillment-status");
        console.log("🔄 Инициализируем fulfillment dropdown", dropdown);
        if (!dropdown) return;

        dropdown.addEventListener("change", handleFulfillmentStatusChange);
    }
    initializeFulfillmentStatusDropdown();

    /* Web-socket map */
    function connectToTechnicianTracking(orderId, onConnectedCallback = null) {
        console.log("🚀 Вызвана connectToTechnicianTracking");
        if (isTrackingConnected) {
            console.log("⚠️ WebSocket уже подключён — выходим");
            return;
        }

        const protocol = window.location.protocol === "https:" ? "wss:" : "ws:";
        const host = window.location.host;
        const wsUrl = `${protocol}//${host}/technicians/orders/${orderId}/track`;

        console.log("🌐 Открываем WebSocket:", wsUrl);
        technicianSocket = new WebSocket(wsUrl);

        technicianSocket.onopen = () => {
            console.log("📡 WebSocket connected");
            isTrackingConnected = true;

            if (typeof onConnectedCallback === "function") {
                onConnectedCallback();
            }
        };

        technicianSocket.onmessage = (event) => {
            const data = JSON.parse(event.data);
            console.log("📡 Данные от WebSocket:", data);

            if (Array.isArray(data)) {
                data.forEach(tech => updateTechnicianLocationOnMap(tech));
            } else {
                updateTechnicianLocationOnMap(data);
            }
        };

        technicianSocket.onerror = (error) => {
            console.error("❌ WebSocket error:", error);
        };

        technicianSocket.onclose = () => {
            console.warn("⚠️ WebSocket connection closed");
            isTrackingConnected = false;
        };
    }
    window.connectToTechnicianTracking = connectToTechnicianTracking;

    function updateTechnicianLocationOnMap(data) {
        console.log("📡 Техник двигается:", data);

        if (!data || !data.TechnicianId) {
            console.warn("⚠️ Получены некорректные данные по технику:", data);
            return;
        }

        const marker = technicianMarkers.find(m =>
            m.__techId?.toString() === data.TechnicianId?.toString()
        );

        if (!marker) {
            console.warn("⚠️ Маркер техника не найден:", data.TechnicianId);
            return;
        }

        const newCoord = [data.Longitude, data.Latitude];

        if (!marker.__path) {
            marker.__path = [newCoord];
        } else {
            marker.__path.push(newCoord);
        }

        marker.setLngLat(newCoord);

        const pathSourceId = `technician-path-source-${data.TechnicianId}`;
        const pathLayerId = `technician-path-layer-${data.TechnicianId}`;

        if (!map.getSource(pathSourceId)) {
            map.addSource(pathSourceId, {
                type: "geojson",
                data: {
                    type: "Feature",
                    geometry: {
                        type: "LineString",
                        coordinates: marker.__path
                    }
                }
            });

            map.addLayer({
                id: pathLayerId,
                type: "line",
                source: pathSourceId,
                layout: {
                    "line-join": "round",
                    "line-cap": "round"
                },
                paint: {
                    "line-color": "red", 
                    "line-width": 3,
                    "line-dasharray": [2, 2] 
                }
            });

            marker.__routeLayerId = pathLayerId;
            marker.__routeSourceId = pathSourceId;
        } else {
            map.getSource(pathSourceId).setData({
                type: "Feature",
                geometry: {
                    type: "LineString",
                    coordinates: marker.__path
                }
            });
        }

        console.log(`✅ Обновлён маршрут и позиция для техника ${data.TechnicianId}`);
    }

    setInterval(fetchOrderDetails, 10000);
});
