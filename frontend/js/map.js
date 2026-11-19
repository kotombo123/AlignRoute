// map.js — Leaflet / OpenStreetMap integration

var TILE_URL = 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png';
var TILE_ATTR = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>';

var DAY_COLORS = [
  '#0EA5E9', // day 1 — primary blue
  '#F59E0B', // day 2 — amber
  '#22C55E', // day 3 — green
  '#8B5CF6', // day 4 — purple
  '#EF4444', // day 5 — red
  '#EC4899', // day 6 — pink
  '#14B8A6', // day 7 — teal
];

/**
 * Initialize a Leaflet map in a container.
 * @param {string} containerId - DOM id of the container element
 * @param {number} lat - Center latitude
 * @param {number} lon - Center longitude
 * @param {number} [zoom=13] - Zoom level
 * @returns {Object|null} Leaflet map instance, or null if Leaflet is not loaded
 */
function initMap(containerId, lat, lon, zoom) {
  if (typeof L === 'undefined') {
    console.warn('[Map] Leaflet not loaded — map features disabled.');
    return null;
  }
  var container = L.DomUtil.get(containerId);
  if (container != null && container._leaflet_id !== null) {
      container.outerHTML = container.outerHTML;
  }
  var map = L.map(containerId, {
    scrollWheelZoom: true,
    zoomControl: true,
  }).setView([lat, lon], zoom || 13);

  L.tileLayer(TILE_URL, {
    attribution: TILE_ATTR,
    maxZoom: 19,
  }).addTo(map);

  return map;
}

/**
 * Create a colored circle marker icon.
 * @param {string} color - Hex color
 * @param {string} [label] - Optional label text
 * @returns {Object} Leaflet divIcon
 */
function coloredIcon(color, label) {
  var html = '<div style="' +
    'background:' + color + ';' +
    'width:28px;height:28px;border-radius:50%;' +
    'border:3px solid #fff;' +
    'box-shadow:0 2px 6px rgba(0,0,0,0.3);' +
    'display:flex;align-items:center;justify-content:center;' +
    'color:#fff;font-size:11px;font-weight:700;' +
    '">' + (label || '') + '</div>';

  return L.divIcon({
    html: html,
    className: 'map-marker-icon',
    iconSize: [28, 28],
    iconAnchor: [14, 14],
    popupAnchor: [0, -16],
  });
}

/**
 * Add a marker to the map.
 * @param {Object} map - Leaflet map
 * @param {number} lat
 * @param {number} lon
 * @param {string} label - Popup text
 * @param {string} [color='#0EA5E9'] - Marker color
 * @param {string} [markerLabel] - Short label on the marker
 * @returns {Object} Leaflet marker
 */
function addMarker(map, lat, lon, label, color, markerLabel) {
  if (!map) return null;
  var icon = coloredIcon(color || '#0EA5E9', markerLabel || '');
  var marker = L.marker([lat, lon], { icon: icon }).addTo(map);
  if (label) {
    marker.bindPopup(label);
  }
  return marker;
}

/**
 * Draw a polyline route between points.
 * @param {Object} map - Leaflet map
 * @param {Array<{lat:number, lon:number}>} points
 * @param {string} [color='#0EA5E9']
 * @returns {Object|null} Leaflet polyline
 */
function drawRoute(map, points, color) {
  if (!map || !points || points.length < 2) return null;

  var latLngs = points.map(function (p) {
    return [p.lat, p.lon];
  });

  return L.polyline(latLngs, {
    color: color || '#0EA5E9',
    weight: 3,
    opacity: 0.7,
    dashArray: '8 4',
  }).addTo(map);
}

/**
 * Fit map bounds to show all markers.
 * @param {Object} map - Leaflet map
 * @param {Array<{lat:number, lon:number}>} points
 */
function fitBounds(map, points) {
  if (!map || !points || points.length === 0) return;

  var bounds = L.latLngBounds(
    points.map(function (p) { return [p.lat, p.lon]; })
  );
  map.fitBounds(bounds, { padding: [40, 40] });
}

/**
 * Remove all layers except the tile layer from the map.
 * @param {Object} map - Leaflet map
 */
function clearLayers(map) {
  if (!map) return;
  map.eachLayer(function (layer) {
    if (!layer._url) { // keep tile layer
      map.removeLayer(layer);
    }
  });
}

/**
 * Get color for a specific day index.
 * @param {number} dayIndex - Zero-based day index
 * @returns {string} Hex color
 */
function getDayColor(dayIndex) {
  return DAY_COLORS[dayIndex % DAY_COLORS.length];
}

// Exports
window.mapUtils = Object.freeze({
  initMap: initMap,
  addMarker: addMarker,
  drawRoute: drawRoute,
  fitBounds: fitBounds,
  clearLayers: clearLayers,
  getDayColor: getDayColor,
  DAY_COLORS: DAY_COLORS,
});
