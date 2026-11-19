// config.js — Gateway overrides
//
// Domyślnie frontend działa przez Caddy (localhost:8080) i używa ścieżek
// względnych (/api, /hub)
//
// Ewentualnie do wypełnienia gdy backend i frontend są na osobnych hostach
// (np. Caddy na CDN, backend na osobnym serwerze).

window.ALIGN_API_BASE = '';   // np. 'https://api.alignroute.app/api'
window.ALIGN_HUB_URL  = '';   // np. 'https://api.alignroute.app/hub'
