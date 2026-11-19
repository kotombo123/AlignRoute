# AlignRoute — Handoff

**Data:** 2026-11-03  
**Projekt:** `/Users/x/Desktop/programowanie/AlignRoute/`  
**Repo:** https://github.com/mila2023/AlignRoute  
**Zespół:** Kamila Wachulec, Mateusz Rutkowski, Konrad Typa, Ziemowit Jakuczun  
**Kurs:** Tworzenie agentów AI w środowisku .NET

---

## Specyfikacja produktu

**AlignRoute** — inteligentny asystent podróży grupowych. Organizator tworzy sesję, AI wyszukuje atrakcje, uczestnicy głosują (Tinder swipe), AI generuje optymalny harmonogram.

| Decyzja | Wartość |
|---|---|
| Maks. uczestników | 20 |
| Maks. dni wycieczki | 7 |
| Kart do głosowania | ~20 |
| Autentykacja | Anonimowe sesje przez link (join code) |
| Konflikty głosów | Większość decyduje (TAK > NIE = wchodzi) |
| Wykluczenia kategorii | Osobiste (tylko dla tego uczestnika) |
| Trigger planu | Organizator ręcznie uruchamia |
| Deployment | Localhost dev → własny mini serwer |

---

## Stack technologiczny

| Warstwa | Technologia |
|---|---|
| Frontend | HTML/CSS/JS (vanilla), Boska serif (Fontshare), Leaflet.js, SignalR client |
| Backend | ASP.NET Core 9 Minimal API, Entity Framework Core, SQLite |
| Real-time | SignalR |
| LLM | PSNC `llm.hpc.psnc.pl` / model `gpt-oss_120b` (OpenAI-compatible, Bearer token) |
| Mapy / atrakcje | OpenStreetMap + Overpass API |
| Gateway | Caddy — serwuje frontend, proxy-uje `/api/*` i `/hub` do backendu |
| Design | taste-skill v2 direction: "page-is-the-object" (seed 1209890472) |

---

## Struktura projektu

```
AlignRoute/
├── HANDOFF.md              ← ten plik
├── backend/
│   ├── AlignRoute.sln
│   └── AlignRoute.Api/
│       ├── Program.cs            — DI, middleware, CORS, auto-migration
│       ├── appsettings.json      — config (LLM key, SQLite, limity)
│       ├── Data/
│       │   └── AppDbContext.cs    — EF Core context + indexes
│       ├── Models/               — Session, Participant, Attraction, Vote, Itinerary, SessionStatus
│       ├── Dtos/                 — CreateSessionRequest, JoinSessionRequest, CastVoteRequest, etc.
│       ├── Services/
│       │   ├── LlmService.cs     — PSNC API (OpenAI-compatible)
│       │   ├── OverpassService.cs — OpenStreetMap POI discovery
│       │   ├── ExplorerAgent.cs   — AI-Eksplorator (wyszukuje + kategoryzuje atrakcje)
│       │   └── PlannerAgent.cs    — AI-Planista (generuje harmonogram)
│       ├── Endpoints/            — SessionEndpoints, AttractionEndpoints, VotingEndpoints, ItineraryEndpoints
│       ├── Hubs/
│       │   └── SessionHub.cs     — SignalR hub (ParticipantJoined, VoteUpdate, PlanGenerated, etc.)
│       └── Migrations/           — InitialCreate (auto-apply on startup)
│
└── frontend/
    ├── index.html               — SPA z 6 ekranami (landing, create, join, lobby, voting, results)
    ├── css/
    │   ├── style.css            — Tokeny (putty/black/teal), layout, sekcje landing page
    │   ├── components.css       — Buttons, forms, cards, demo swipe, product UI fragments
    │   ├── screens.css          — Ekrany SPA (create, join, lobby, voting, results)
    │   ├── animations.css       — Crossfade hero, entrance animations, swipe, reduced-motion
    │   └── responsive.css       — Mobile-first breakpoints (640/768/1024/1280)
    ├── js/
    │   ├── app.js               — Routing, state, demo card, entrance observer
    │   ├── i18n.js              — PL/EN tłumaczenia (58+ kluczy na język)
    │   ├── api.js               — REST client (localhost:5000/api)
    │   ├── signalr-client.js    — SignalR z auto-reconnect
    │   ├── swipe.js             — Touch/mouse drag engine
    │   ├── voting.js            — Karty głosowania, wykluczenia, sidebar
    │   ├── results.js           — Podsumowanie głosów, timeline, mapa
    │   ├── map.js               — Leaflet/OSM markers, routes
    │   └── utils.js             — DOM helpers, clipboard, debounce
    └── .taste-skill/            — Direction sheet, inspect screenshots, report
```

---

## API Endpoints

### Sesje
- `POST /api/sessions` — Tworzenie sesji (destination, durationDays, organizerNickname, language)
- `GET /api/sessions/{joinCode}` — Info o sesji (publiczne, do join page)
- `POST /api/sessions/{joinCode}/join` — Dołączenie (nickname)
- `GET /api/sessions/{sessionId}/status` — Status + lista uczestników

### Atrakcje (AI-Explorer)
- `POST /api/sessions/{sessionId}/explore` — Trigger eksploracji (tylko organizator)
- `GET /api/sessions/{sessionId}/attractions` — Lista atrakcji

### Głosowanie
- `GET /api/sessions/{sessionId}/cards/{participantId}` — Karty (minus wykluczone kategorie)
- `POST /api/sessions/{sessionId}/vote` — Oddaj głos (participantId, attractionId, isPositive)
- `PUT /api/sessions/{sessionId}/participants/{participantId}/exclusions` — Wykluczenia
- `PUT /api/sessions/{sessionId}/participants/{participantId}/done` — Zakończ głosowanie

### Harmonogram (AI-Planner)
- `POST /api/sessions/{sessionId}/generate-plan` — Generuj plan (tylko organizator)
- `GET /api/sessions/{sessionId}/itinerary` — Pobierz wygenerowany plan

### SignalR Hub: `/hubs/session`
Events: `ParticipantJoined`, `VoteUpdate`, `ParticipantFinished`, `ExplorationComplete`, `PlanGenerated`

---

## Konfiguracja

### Backend (`appsettings.json`)
```json
{
  "ConnectionStrings": { "DefaultConnection": "Data Source=alignroute.db" },
  "LlmSettings": {
    "BaseUrl": "https://llm.hpc.psnc.pl",
    "Model": "gpt-oss_120b",
    "ApiKey": ""
  },
  "SessionSettings": { "MaxParticipants": 20, "MaxAttractions": 20, "MaxDurationDays": 7 }
}
```

Klucz API: ustaw `LLM_API_KEY` jako zmienną środowiskową lub wpisz w `appsettings.json` → `LlmSettings:ApiKey`.

### PSNC LLM API
```bash
curl -X POST https://llm.hpc.psnc.pl/v1/chat/completions \
  -H "Authorization: Bearer <YOUR_API_KEY>" \
  -H "Content-Type: application/json" \
  -d '{"model": "gpt-oss_120b", "messages": [{"role": "user", "content": "Hello!"}]}'
```

### Frontend (`js/api.js`)
```javascript
const API_BASE = 'http://localhost:5000/api';
```

---

## Uruchomienie

### Wymagania wstępne
```bash
brew install caddy   # jednorazowo
```

### Backend
```bash
cd backend/AlignRoute.Api
export LLM_API_KEY="twój-klucz-psnc"
dotnet run
# → http://localhost:5038 (nie otwieraj bezpośrednio)
```

### Caddy Gateway
```bash
cd AlignRouteProj   # katalog główny (tam gdzie Caddyfile)
caddy run
# → https://localhost:8080  ← tutaj otwierasz aplikację
```

Caddy serwuje pliki frontendu z `./frontend/` i proxy-uje `/api/*` oraz `/hub` do backendu na `:5038`.

Frontend działa bez backendu w **mock mode** (8 atrakcji Krakowa, 3-dniowy itinerarium).

---

## Design — taste-skill v2

**Kierunek:** `page-is-the-object` (seed 1209890472)  
**Idea:** „Karta do swipe'owania — gest, który zamienia opinii w trasę — staje się artefaktem z papieru, z którego zbudowana jest strona."

| Element | Decyzja |
|---|---|
| Font | Boska (Fontshare) — high-contrast serif |
| Paleta | Putty `#dedad3`, black `#0a0a0a`, teal accent `#256f82` |
| Hero | Pasiasta karta (ink/paper band) + sticky zdjęcie architektoniczne |
| How it works | Paragon/itinerarium z zigzag krawędzią (clip-path) |
| Features | 3 stackujące się kolorowe arkusze (sticky, 72px offset) |
| Team | List od zespołu na papierowej karcie ze scallop edge |
| CTA | Pas 3 zdjęć + teal band z podkreślonym linkiem |
| Footer | Siatka kafelków z wordmarkiem 9vw |
| Live thing | Demo swipe card (Smok Wawelski) na landing page |
| Motion | Slow crossfade zdjęć hero (3 img, 18s cycle) + entrance stagger |

### Taste-skill gate status (ostatni run)

**Lint:** 0 FAIL, 1 WARN (em-dashes)

**Inspect:** 2 FAIL, 7 WARN

| Typ | Kod | Opis | Fix |
|---|---|---|---|
| FAIL | `off-centre` | Strona asymetryczna na 1440px (20px L vs 151px R) | Feature sheets potrzebują `max-width: 1400px; margin: 0 auto` na wewnętrznym content, nie na samym sheet |
| FAIL | `covered-text` | Tekst na feature sheets zakrywany przez kolejny sticky sheet | Zamierzone zachowanie stacking — sheet name musi mieć wyższy z-index lub zmieniać visibility przy opuszczaniu viewportu |
| WARN | `size-count` | 14 font sizes (zalecane ~5) | Zredukować do 5 kroków: display, head, lead, body, small |
| WARN | `grey-body` | Body text szary (4.5:1 contrast) | `.sheet__desc` ma `opacity: 0.8` — usunąć, użyć full ink |
| WARN | `no-footage` | Brak video | Dodać muted loop z `assets.mjs --video` lub pominąć |
| WARN | `blank-gap` | 464px przerwa w pionie | Zmniejszyć `--section-gap` lub dodać content |
| WARN | `untitled-section` | Sekcja team bez heading | Dodać krótki h2 nad letter |
| WARN | `still-page` | 13 moving elements (zalecane 16+) | Dodać więcej `.entrance` klas + moving picture |
| WARN | `off-centre` @768px | Asymetria marginesów tablet | Sprawdzić padding/scrollbar-gutter |

### Narzędzia taste-skill
```bash
# Pokaż direction sheet
cd frontend && node ~/.claude/skills/design-taste-frontend/scripts/draw.mjs --show

# Lint source
node ~/.claude/skills/design-taste-frontend/scripts/lint.mjs css/ index.html

# Inspect (otwiera Chrome, robi screeny)
node ~/.claude/skills/design-taste-frontend/scripts/inspect.mjs index.html --out .taste-skill/inspect

# Szukaj zdjęć w open collections
node ~/.claude/skills/design-taste-frontend/scripts/assets.mjs "krakow old town"

# Screeny w:
.taste-skill/inspect/desktop-fold.jpg
.taste-skill/inspect/desktop-page-01.jpg
.taste-skill/inspect/mobile-fold.jpg
```

---

## Co zostało do zrobienia

### Priorytet 1 — Naprawy inspektora ✅ (2026-11-03)
- [x] Fix `off-centre` — dodano `scrollbar-gutter: stable both-edges` do `html`
- [x] Fix `grey-body` — `hero__sentence` zmieniony na `color: var(--ink)`, `.sheet__desc` już miało `color: inherit`
- [x] Fix `untitled-section` — dodano klucz `letter.heading` do i18n, h2 już istnieje w HTML
- [x] Fix `size-count` — tokeny `--step-*` już zdefiniowane, hardcoded rozmiarów nie znaleziono
- [x] Dodano `.entrance` do 20+ elementów (sheet names, CTA photos, footer tiles)

### Priorytet 2 — Integracja front↔back ✅ (2026-11-05)
- [x] Podłączony klucz API PSNC (`LLM_API_KEY` env var)
- [x] Naprawiony port: backend na 5038, frontend zaktualizowany (`api.js`, `signalr-client.js`)
- [x] CORS — zawężony do `localhost:8080` (Caddy gateway); na produkcji: domena Caddy
- [x] Naprawiony payload `createSession` — pole `nickname` → `organizerNickname`
- [x] Naprawione modele EF Core — `IReadOnlyList<T>` → `List<T>` (fix "Collection was of a fixed size")
- [x] Obsługa błędów — LLM timeout/błąd, Overpass timeout, brak atrakcji
- [x] Frontend: toast przy błędach sieci, fallback mock mode z powiadomieniem
- [x] SignalR: toast przy rozłączeniu/reconnect
- [ ] Przetestować full flow: create session → explore → vote → generate plan
- [ ] Sprawdzić SignalR real-time (2+ przeglądarki jednocześnie)

### Priorytet 3 — Polish ✅ (2026-11-03)
- [x] Responsive tablet (768px) — naprawiono `--margin: 32px` przy 768px+
- [ ] Dodać video loop (opcjonalnie)
- [x] Polskie znaki diakrytyczne w i18n — wszystkie klucze PL poprawione
- [x] WCAG AA — `hero__sentence` zmieniony na `var(--ink)`, palette weryfikowana
- [x] Print styles — kompletny `@media print` z obsługą features sheets, linkami, page-breaks

### Priorytet 4 — Deploy
- [ ] Git init + push do `mila2023/AlignRoute`
- [ ] README.md z instrukcjami dla zespołu (patrz `PROJECT.md` jako baza)
- [ ] Deploy backend na mini serwer
- [ ] CORS — zaktualizować `WithOrigins(...)` w `Program.cs` o domenę produkcyjną Caddy

## Udostępnianie przez ngrok (tymczasowe testy)

Wystarczy **jeden** tunel — Caddy obsługuje frontend i API na jednym porcie:

```bash
ngrok http 8080   # → https://abc123.ngrok-free.app
```

`frontend/config.js` zostaje pusty — nie wpisuj nic. Caddy przekierowuje
`/api/*` i `/hub` do lokalnego backendu automatycznie.

---

## Otwarte pytania z requirements.md (rozwiązane)

| Pytanie | Decyzja |
|---|---|
| Ile uczestników? | Maks. 20 |
| Czy wszyscy muszą zagłosować? | Nie — organizator triggeruje ręcznie |
| Brak atrakcji? | Error handling w ExplorerAgent |
| Konflikty? | Większość głosów |
| Zmiana głosu? | Unique constraint (AttractionId, ParticipantId) — jeden głos per atrakcja |
| Wykluczenia grupowe? | Nie — osobiste only |
| Modyfikacja planu? | Nie w MVP |
| Notyfikacje? | SignalR real-time |

---

## Kontekst sesji

- Taste-skill zainstalowany: `~/.claude/skills/design-taste-frontend` → `~/.claude/taste-skill-repo/`
- Update taste-skill: `cd ~/.claude/taste-skill-repo && git pull`
- Direction JSON: `frontend/.taste-skill/direction.json`
- Screeny inspektora: `frontend/.taste-skill/inspect/`
