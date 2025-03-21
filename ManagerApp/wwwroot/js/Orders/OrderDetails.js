document.addEventListener("DOMContentLoaded", async function () {
    const mapElement = document.getElementById("map");
    if (!mapElement) return;

    const routesData = mapElement.dataset.routes;
    if (!routesData) return;

    const initialRoutes = JSON.parse(routesData);

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
        "#FF5733", "#33FF57", "#5733FF", "#FF33A8", "#33FFF5", "#F5FF33",
        "#FF8C00", "#ADFF2F", "#8A2BE2", "#DC143C", "#00CED1", "#32CD32"
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
                const coordinates = e.lngLat;
                const properties = e.features[0].properties;
                new maplibregl.Popup()
                    .setLngLat(coordinates)
                    .setHTML(`<strong>Technician:</strong> ${properties.technicianName}<br><strong>Phone:</strong> ${properties.phoneNumber}`)
                    .addTo(map);
            });

            const startPoint = points[0];
            const technicianMarker = new maplibregl.Marker({ color: "blue" })
                .setLngLat(startPoint)
                .setPopup(new maplibregl.Popup().setHTML(`<strong>${route.technicianName}</strong><br>${route.phoneNumber}`))
                .addTo(map);
            technicianMarkers.push(technicianMarker);

            const endPoint = points[points.length - 1];
            const installationMarker = new maplibregl.Marker({ color: "red" })
                .setLngLat(endPoint)
                .setPopup(new maplibregl.Popup().setHTML("Installation Point"))
                .addTo(map);
            installationMarkers.push(installationMarker);

            const warehousePoint = route.routePoints.find(p => p.isStopPoint);
            if (warehousePoint) {
                const warehouseMarker = new maplibregl.Marker({ color: "#8B4513" }) 
                    .setLngLat([warehousePoint.longitude, warehousePoint.latitude])
                    .setPopup(new maplibregl.Popup().setHTML("Warehouse"))
                    .addTo(map);
                warehouseMarkers.push(warehouseMarker);
            }

            const legendItem = document.createElement("li");
            legendItem.innerHTML = `<input type="checkbox" checked data-route-id="${routeId}"> <span style="color: ${color}; font-weight: bold;">⬤</span> ${route.technicianName}`;
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
});
