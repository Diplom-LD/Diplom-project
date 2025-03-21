document.addEventListener("DOMContentLoaded", function () {
    const mapElement = document.getElementById("map");

    if (!mapElement) return;

    const routesData = mapElement.dataset.routes;
    if (!routesData) return;

    const initialRoutes = JSON.parse(routesData);
    const map = L.map('map').setView([47.0, 28.85], 13);

    // Базовая карта
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        maxZoom: 18,
    }).addTo(map);

    const routeColors = ["#007bff", "#28a745", "#ff5722", "#6f42c1"];
    let bounds = [];

    initialRoutes.forEach((route, index) => {
        const color = routeColors[index % routeColors.length];
        const points = route.routePoints.map(p => [p.latitude, p.longitude]);

        const polyline = L.polyline(points, { color, weight: 4 }).addTo(map);
        bounds.push(...points);

        const technicianIcon = L.divIcon({ className: 'tech-icon', html: '👷‍♂️' });
        const clientIcon = L.divIcon({ className: 'client-icon', html: '📍' });
        const warehouseIcon = L.divIcon({ className: 'warehouse-icon', html: '🏭' });

        const startPoint = points[0];
        const endPoint = points[points.length - 1];

        L.marker(startPoint, { icon: technicianIcon })
            .bindPopup(`<strong>${route.technicianName}</strong><br>${route.phoneNumber}`)
            .addTo(map);

        const warehousePoint = route.routePoints.find(p => p.isStopPoint);
        if (warehousePoint) {
            L.marker([warehousePoint.latitude, warehousePoint.longitude], { icon: warehouseIcon })
                .bindPopup("Склад")
                .addTo(map);
        }

        L.marker(endPoint, { icon: clientIcon })
            .bindPopup("Установка кондиционера")
            .addTo(map);
    });

    if (bounds.length > 0) {
        map.fitBounds(bounds);
    }
});
