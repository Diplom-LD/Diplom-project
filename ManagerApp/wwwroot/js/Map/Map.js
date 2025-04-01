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
        marker.getElement().style.zIndex = type === "technician" ? "20" : "10";

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

    const legend = document.querySelector(".map-legend");
    const toggleBtn = document.getElementById("toggle-legend-btn");
    const legendTitleText = document.querySelector(".legend-title-text");

    if (legend && toggleBtn && legendTitleText) {
        toggleBtn.addEventListener("click", () => {
            legend.classList.toggle("hidden");

            const isHidden = legend.classList.contains("hidden");
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

    /* Оставление маркеров*/
    let isDrawMode = false;
    const tempMarkers = [];

    const drawModeCheckbox = document.getElementById("toggle-draw-mode");

    if (drawModeCheckbox) {
        drawModeCheckbox.addEventListener("change", () => {
            isDrawMode = drawModeCheckbox.checked;
            map.getCanvas().style.cursor = isDrawMode ? "crosshair" : "";

            if (!isDrawMode) {
                clearTempMarkers();
            }
        });
    }

    map.on("click", (e) => {
        if (!isDrawMode) return;

        const marker = new maplibregl.Marker({ color: "orange" })
            .setLngLat(e.lngLat)
            .addTo(map);

        tempMarkers.push(marker);
    });

    function clearTempMarkers() {
        tempMarkers.forEach(marker => marker.remove());
        tempMarkers.length = 0; 
    }

    /* Линейка для измерений */
    let rulerPoints = [];
    let isDrawing = false;

    map.getCanvas().addEventListener("contextmenu", (e) => e.preventDefault());

    map.on("mousedown", (e) => {
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
        if (!isDrawing) return;

        const currentPoint = [e.lngLat.lng, e.lngLat.lat];
        const dynamicPoints = [...rulerPoints, currentPoint];

        updateRulerLine(dynamicPoints);

        const distance = calculateDistance(dynamicPoints);
        updateRulerLabel(distance);
    });

    map.on("mouseup", (e) => {
        if (e.originalEvent.button !== 1 || !isDrawing) return;

        isDrawing = false;
        rulerPoints.push([e.lngLat.lng, e.lngLat.lat]);
        updateRulerLine();

        const distance = calculateDistance(rulerPoints);
        updateRulerLabel(distance);
    });

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

});
