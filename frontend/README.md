# AlignRoute Frontend

Smart group travel planning assistant — discover attractions, vote together, and get an optimized itinerary.

## Quick Start

The frontend is served by **Caddy**, which also proxies API and SignalR traffic
to the .NET backend. Run both from the project root:

```bash
# Terminal 1 — Backend (from backend/AlignRoute.Api/)
dotnet run

# Terminal 2 — Caddy Gateway (from project root, where Caddyfile lives)
caddy run

# Open in browser:
# http://localhost:8080
```

Install Caddy once: `brew install caddy`

> **Do not** open `index.html` directly or use `npx serve` / `python3 -m http.server` —
> those bypass the gateway and break API and SignalR connections.

Without the backend running, the app falls back to **mock mode** with sample Krakow data.

## File Structure

```
frontend/
├── index.html              Main entry point (SPA)
├── README.md               This file
├── css/
│   ├── style.css           Layout, typography, design tokens
│   ├── components.css      Buttons, forms, cards, badges
│   ├── animations.css      Swipe transitions, loading states
│   └── responsive.css      Mobile-first media queries
├── js/
│   ├── app.js              Main app: state, routing, screen logic
│   ├── i18n.js             Polish/English translations
│   ├── api.js              Backend API communication
│   ├── signalr-client.js   Real-time SignalR hub connection
│   ├── swipe.js            Tinder-style card swipe engine
│   ├── map.js              Leaflet/OpenStreetMap integration
│   └── utils.js            DOM helpers and utilities
├── assets/
│   └── icons/              SVG icons (inline in HTML)
└── lib/                    CDN dependencies (loaded in HTML)
```

## Dependencies (CDN)

- **Inter font** — Google Fonts
- **Leaflet.js 1.9.4** — OpenStreetMap maps
- **SignalR 8.0.0** — Real-time communication

## Backend API

API and SignalR are routed through Caddy using relative paths — no hardcoded
URLs needed. The gateway config lives in `Caddyfile` at the project root.

If you need to point to a backend on a different host, override in `config.js`:

```javascript
window.ALIGN_API_BASE = 'https://your-host/api';
window.ALIGN_HUB_URL  = 'https://your-host/hub';
```

## Language Support

The app supports Polish (PL) and English (EN). Toggle via the button in the navigation bar. Language preference is saved to localStorage.

All UI strings use the i18n system in `js/i18n.js`. HTML elements use `data-i18n` attributes for automatic text replacement.

## Screens

1. **Landing** — Hero, how-it-works, features, team
2. **Create Session** — Destination, duration, nickname form
3. **Join Session** — Enter via `?join=CODE` URL parameter
4. **Lobby** — Session code, participant list, start button
5. **Voting** — Tinder-style swipe cards with category exclusions
6. **Results** — Vote summary, day-by-day timeline, full map

## Team

- Kamila Wachulec
- Mateusz Rutkowski
- Konrad Typa
- Ziemowit Jakuczun
