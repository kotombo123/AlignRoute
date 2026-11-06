# AlignRoute Frontend

Smart group travel planning assistant — discover attractions, vote together, and get an optimized itinerary.

## Quick Start

No build tools required. Open the app directly in a browser:

```bash
# Option 1: Open directly
open index.html

# Option 2: Use any static server
npx serve .
# or
python3 -m http.server 8080
```

The app expects the backend at `http://localhost:5000`. Without the backend, the app runs in **mock mode** with sample Krakow data.

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

Configure the backend URL in `js/api.js`:

```javascript
var API_BASE = 'http://localhost:5000/api';
```

The SignalR hub URL is in `js/signalr-client.js`:

```javascript
var HUB_URL = 'http://localhost:5000/hubs/session';
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
