document.addEventListener("DOMContentLoaded", async () => {
    console.log("🗺️ Инициализация карты...");

    const mapContainer = document.getElementById("map");
    if (!mapContainer) return;

    const rawData = mapContainer.dataset.locations;
    if (!rawData) {
        console.error("❌ Нет данных в data-locations");
        return;
    }

    let locations;
    try {
        locations = JSON.parse(rawData);
        console.log("📍 Locations loaded:", locations);
    } catch (err) {
        console.error("❌ Ошибка парсинга locations:", err);
        return;
    }

    let apiKey;
    try {
        const apiResponse = await fetch("/maps/api-key");
        const apiData = await apiResponse.json();
        apiKey = apiData.apiKey;
    } catch (err) {
        console.error("❌ Ошибка загрузки API ключа:", err);
        return;
    }

    const map = new maplibregl.Map({
        container: "map",
        style: `https://api.maptiler.com/maps/streets/style.json?key=${apiKey}`,
        center: [28.85, 47.01],
        zoom: 11
    });

    map.addControl(new maplibregl.NavigationControl());
    map.addControl(new maplibregl.FullscreenControl());

    const bounds = new maplibregl.LngLatBounds();
    const warehouseMarkers = [];
    const technicianMarkers = [];

    function createMarker(type, color, lngLat, popupHtml) {
        const popup = new maplibregl.Popup({ offset: 25 }).setHTML(popupHtml);
        const marker = new maplibregl.Marker({ color })
            .setLngLat(lngLat)
            .setPopup(popup)
            .addTo(map);

        marker.customType = type;
        const markerEl = marker.getElement();
        markerEl.style.zIndex = type === "technician" ? "20" : "10";

        if (type === "warehouse") warehouseMarkers.push(marker);
        if (type === "technician") technicianMarkers.push(marker);

        bounds.extend(lngLat);
    }

    locations.warehouses.forEach(w => {
        createMarker("warehouse", "#8B4513", [w.longitude, w.latitude], `
            <div class="popup-card">
                <div class="popup-name">🏭 <strong>${w.name}</strong></div>
                <div class="popup-address">${w.address}</div>
                <div class="popup-phone">📞 ${w.phoneNumber}</div>
                <div class="popup-contact">👤 ${w.contactPerson}</div>
            </div>
        `);
    });

    locations.technicians.forEach(t => {
        createMarker("technician", "#0074D9", [t.longitude, t.latitude], `
            <div class="popup-card">
                <div class="popup-name">👨‍🔧 <strong>${t.fullName}</strong></div>
                <div class="popup-address">${t.address}</div>
                <div class="popup-phone">📞 ${t.phoneNumber}</div>
                <div class="popup-email">✉️ ${t.email}</div>
            </div>
        `);
    });

    if (!bounds.isEmpty()) {
        map.fitBounds(bounds, { padding: 60 });
    }

    document.addEventListener("change", (e) => {
        if (e.target.id === "toggle-warehouse") {
            warehouseMarkers.forEach(m => {
                m.getElement().style.display = e.target.checked ? "block" : "none";
            });
        }
        if (e.target.id === "toggle-technician") {
            technicianMarkers.forEach(m => {
                m.getElement().style.display = e.target.checked ? "block" : "none";
            });
        }
    });

    document.addEventListener("fullscreenchange", () => {
        const legend = document.querySelector(".map-legend");
        const fsElement = document.fullscreenElement;
        const canvasContainer = fsElement?.querySelector(".maplibregl-canvas-container");

        if (legend && canvasContainer && !canvasContainer.contains(legend)) {
            canvasContainer.appendChild(legend);
        } else {
            const original = document.querySelector(".map-container");
            if (legend && original && !original.contains(legend)) {
                original.appendChild(legend);
            }
        }
    });

    /* Легенда карты */
    const legend = document.querySelector(".map-legend");
    const toggleBtn = document.getElementById("toggle-legend-btn");
    const legendTitleText = document.querySelector(".legend-title-text");

    if (legend && toggleBtn && legendTitleText) {
        toggleBtn.addEventListener("click", () => {
            const isHidden = legend.classList.toggle("hidden");

            toggleBtn.textContent = isHidden ? "📑" : "✖";
            toggleBtn.title = isHidden ? "Show Legend" : "Hide Legend";

            legendTitleText.style.display = isHidden ? "none" : "inline";

            const mapTitle = document.querySelector(".map-title");
            if (mapTitle) {
                mapTitle.style.display = isHidden ? "none" : "block";
            }
        });
    }

    map.on("popupclose", (e) => {
        const popup = e.popup;
        const targetMarker =
            warehouseMarkers.find(m => m.getPopup() === popup) ||
            technicianMarkers.find(m => m.getPopup() === popup);

        if (targetMarker) {
            targetMarker.getElement().style.display = "none";
        }
    });


    /* Оставление маркеров */
    let isDrawMode = false;
    let pendingLngLat = null;
    const tempMarkers = [];

    const drawModeCheckbox = document.getElementById("toggle-draw-mode");
    const modal = document.getElementById("customMarkerModal");
    const input = document.getElementById("markerTitleInput");
    const confirmBtn = document.getElementById("markerConfirm");
    const cancelBtn = document.getElementById("markerCancel");

    if (drawModeCheckbox) {
        drawModeCheckbox.addEventListener("change", () => {
            isDrawMode = drawModeCheckbox.checked;
            map.getCanvas().style.cursor = isDrawMode ? "crosshair" : "";

            if (isDrawMode) {
                tempMarkers.forEach(m => m.getElement().style.display = "block");
            }
        });
    }

    map.on("click", async (e) => {
        if (!isDrawMode) return;

        const target = e.originalEvent.target;
        const isInsideMarker = target.closest('.maplibregl-marker');
        const isInsidePopup = target.closest('.maplibregl-popup');
        if (isInsideMarker || isInsidePopup) return;

        pendingLngLat = e.lngLat;
        input.value = "Новая точка";

        modal.style.left = `${e.originalEvent.clientX}px`;
        modal.style.top = `${e.originalEvent.clientY}px`;

        modal.classList.remove("hidden");
        input.focus();
    });

    confirmBtn.addEventListener("click", async () => {
        const title = input.value.trim();
        if (!title || !pendingLngLat) return;

        let address = "Адрес не найден";
        try {
            const res = await fetch(`https://nominatim.openstreetmap.org/reverse?format=jsonv2&lat=${pendingLngLat.lat}&lon=${pendingLngLat.lng}`);
            const data = await res.json();
            if (data?.display_name) address = data.display_name;
        } catch {
            console.warn("❗ Ошибка получения адреса");
        }

        const popupContent = document.createElement("div");
        popupContent.className = "popup-card";
        popupContent.innerHTML = `
        <div class="popup-name">📍 <strong>${title}</strong></div>
        <div class="popup-address">${address}</div>
        <button class="delete-marker-btn">🗑️ Удалить</button>
    `;

        const popup = new maplibregl.Popup({ offset: 25 }).setDOMContent(popupContent);
        const marker = new maplibregl.Marker({ color: "orange" })
            .setLngLat(pendingLngLat)
            .setPopup(popup)
            .addTo(map);

        popupContent.querySelector(".delete-marker-btn").addEventListener("click", () => {
            marker.remove();
            const index = tempMarkers.indexOf(marker);
            if (index !== -1) tempMarkers.splice(index, 1);
        });

        if (!isDrawMode) marker.getElement().style.display = "none";

        tempMarkers.push(marker);
        closeModal();
    });

    cancelBtn.addEventListener("click", closeModal);

    function closeModal() {
        modal.classList.add("hidden");
        pendingLngLat = null;
    }

    function clearTempMarkers() {
        tempMarkers.forEach(marker => {
            marker.getElement().style.display = "none";
        });
    }


    /* Линейка для измерений */
    let rulerPoints = [];
    let isRulerModeEnabled = false;
    let isDrawing = false;

    map.getCanvas().addEventListener("contextmenu", (e) => e.preventDefault());

    map.on("mousedown", (e) => {
        if (!isRulerModeEnabled) return;

        const button = e.originalEvent.button;
        if (button === 1) {
            isDrawing = true;
            rulerPoints = [[e.lngLat.lng, e.lngLat.lat]];
            updateRulerLine();
        }

        if (button === 2) {
            clearRuler();
        }
    });

    map.on("mousemove", (e) => {
        if (!isRulerModeEnabled || !isDrawing) return;

        const currentPoint = [e.lngLat.lng, e.lngLat.lat];
        const dynamicPoints = [...rulerPoints, currentPoint];

        updateRulerLine(dynamicPoints);

        const distance = calculateDistance(dynamicPoints);
        updateRulerLabel(distance);
    });

    map.on("mouseup", (e) => {
        if (!isRulerModeEnabled || !isDrawing || e.originalEvent.button !== 1) return;

        isDrawing = false;
        rulerPoints.push([e.lngLat.lng, e.lngLat.lat]);
        updateRulerLine();

        const distance = calculateDistance(rulerPoints);
        updateRulerLabel(distance);
    });

    const toggleRuler = document.getElementById("toggle-ruler-mode");

    if (toggleRuler) {
        toggleRuler.addEventListener("change", () => {
            isRulerModeEnabled = toggleRuler.checked;
            isDrawing = false;
            map.getCanvas().style.cursor = isRulerModeEnabled ? "crosshair" : "";

            if (!isRulerModeEnabled) {
                clearRuler();
            }
        });
    }

    function updateRulerLine(points = rulerPoints) {
        const geojson = {
            type: "Feature",
            geometry: {
                type: "LineString",
                coordinates: points
            }
        };

        if (map.getSource("ruler-line")) {
            map.getSource("ruler-line").setData(geojson);
        } else {
            map.addSource("ruler-line", {
                type: "geojson",
                data: geojson
            });

            map.addLayer({
                id: "ruler-line-layer",
                type: "line",
                source: "ruler-line",
                layout: {
                    "line-cap": "round",
                    "line-join": "round"
                },
                paint: {
                    "line-color": "red",
                    "line-width": 3,
                    "line-dasharray": [2, 2]
                }
            });
        }
    }

    function calculateDistance(coords) {
        const R = 6371e3;
        let total = 0;

        for (let i = 1; i < coords.length; i++) {
            const [lng1, lat1] = coords[i - 1];
            const [lng2, lat2] = coords[i];
            const φ1 = lat1 * Math.PI / 180;
            const φ2 = lat2 * Math.PI / 180;
            const Δφ = (lat2 - lat1) * Math.PI / 180;
            const Δλ = (lng2 - lng1) * Math.PI / 180;

            const a = Math.sin(Δφ / 2) ** 2 +
                Math.cos(φ1) * Math.cos(φ2) * Math.sin(Δλ / 2) ** 2;
            const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
            total += R * c;
        }

        return total;
    }

    function updateRulerLabel(meters) {
        const mapTitle = document.querySelector(".map-title");
        if (mapTitle) {
            const km = (meters / 1000).toFixed(2);
            mapTitle.textContent = `📏 Distance: ${km} km`;
        }
    }

    function clearRuler() {
        rulerPoints = [];
        isDrawing = false;

        if (map.getLayer("ruler-line-layer")) {
            map.removeLayer("ruler-line-layer");
        }
        if (map.getSource("ruler-line")) {
            map.removeSource("ruler-line");
        }

        const mapTitle = document.querySelector(".map-title");
        if (mapTitle) {
            mapTitle.textContent = "🗺️ Map of Technicians and Warehouses";
        }
    }

    document.addEventListener("click", (e) => {
        if (e.target.classList.contains("maplibregl-popup-close-button")) {
            const allPopups = document.querySelectorAll(".maplibregl-popup");
            allPopups.forEach(popup => popup.remove());
        }
    });

});
