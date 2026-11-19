# AlignRoute — Dokumentacja projektu

> Inteligentny asystent podróży grupowych z głosowaniem i planowaniem tras przez AI.

---

## Cel projektu

Planowanie grupowej wycieczki to chaos — każdy ma inne pomysły, nikt się nie może dogadać. AlignRoute rozwiązuje ten problem w trzech krokach:

1. **AI odkrywa atrakcje** dla wybranego miasta
2. **Każdy uczestnik głosuje** (Tinder-style swipe) na te, które go interesują
3. **AI układa optymalny harmonogram** na podstawie wyników głosowania

---

## Stack technologiczny

| Warstwa | Technologia | Do czego |
|---|---|---|
| **Frontend** | HTML / CSS / JavaScript (vanilla) | Interfejs użytkownika, SPA z 6 ekranami |
| **Czcionka** | Boska (Fontshare) | Typografia — high-contrast serif |
| **Mapy** | Leaflet.js + OpenStreetMap | Wyświetlanie markerów atrakcji |
| **Backend** | ASP.NET Core 9, C#, Minimal API | REST API, logika biznesowa |
| **Baza danych** | SQLite + Entity Framework Core | Przechowywanie sesji, głosów, planów |
| **Real-time** | SignalR (WebSockets) | Live aktualizacje podczas głosowania |
| **LLM** | PSNC `llm.hpc.psnc.pl`, model `gpt-oss_120b` | Generowanie opisów i harmonogramów |
| **POI** | Overpass API (OpenStreetMap) | Pobieranie punktów turystycznych |

---

## Architektura

```
┌─────────────────────────────────────────────────┐
│                   PRZEGLĄDARKA                  │
│                                                 │
│  Landing → Create → Lobby → Voting → Results   │
│                                                 │
│  api.js ──────── REST ────────────────────────► │
│  signalr.js ───── WebSocket (SignalR) ────────► │
└───────────────────────────┬─────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────┐
│              ASP.NET CORE 9  (port 5038)        │
│                                                 │
│  POST /api/sessions          ← Utwórz sesję     │
│  POST /api/sessions/join     ← Dołącz do sesji  │
│  POST /api/sessions/explore  ← Uruchom agenta   │
│  POST /api/sessions/vote     ← Oddaj głos       │
│  POST /api/sessions/generate-plan ← Plan AI     │
│                                                 │
│  ┌──────────────┐    ┌──────────────────────┐   │
│  │ ExplorerAgent│    │    PlannerAgent       │   │
│  └──────┬───────┘    └──────────┬───────────┘   │
│         │                       │               │
└─────────┼───────────────────────┼───────────────┘
          │                       │
          ▼                       ▼
   ┌──────────────┐      ┌────────────────┐
   │ Overpass API │      │  PSNC LLM API  │
   │ (OpenStreet  │      │ gpt-oss_120b   │
   │    Map)      │      │                │
   └──────────────┘      └────────────────┘
```

---

## Agenci AI

Projekt zawiera dwóch agentów zaimplementowanych jako serwisy C# (`Services/`).

### ExplorerAgent — odkrywanie atrakcji

**Uruchamiany:** gdy organizator kliknie „Rozpocznij eksplorację" w poczekalni.

**Przepływ działania:**

```
1. Overpass API
   └─ zapytanie: POI w okolicach miasta (turystyka, parki, restauracje)
   └─ wynik: lista surowych punktów z nazwą, kategorią, współrzędnymi

2. LLM (gpt-oss_120b)
   └─ prompt: "masz te POI — wybierz top 20 najciekawszych dla turystów,
               dodaj przekonujący 2-zdaniowy opis w języku PL/EN,
               przypisz kategorię (museum, monument, park, restaurant...)"
   └─ wynik: JSON array z wzbogaconymi atrakcjami

3. Zapis do bazy (SQLite)
   └─ każda atrakcja → obiekt Attraction z Id, SessionId, Name, Description,
                        Category, Latitude, Longitude

4. SignalR → ExplorationComplete → wszyscy uczestnicy dostają powiadomienie
```

**Fallback:** jeśli Overpass nie znajdzie POI (miasto nieznane w OSM), LLM generuje atrakcje samodzielnie z własnej wiedzy.

---

### PlannerAgent — generowanie harmonogramu

**Uruchamiany:** gdy organizator kliknie „Zakończ głosowanie i wygeneruj plan".

**Przepływ działania:**

```
1. Zliczenie głosów
   └─ atrakcje gdzie TAK > NIE → lista zwycięskich atrakcji

2. LLM (gpt-oss_120b)
   └─ prompt: "ułóż N-dniowy plan dla tych atrakcji w [miasto].
               Grupuj pobliskie miejsca na ten sam dzień,
               minimalizuj odległości, zaproponuj godziny i czas wizyty."
   └─ wynik: JSON array — każdy dzień z listą atrakcji, godzinami, notatkami

3. Zapis do bazy (SQLite)
   └─ obiekt Itinerary z PlanJson

4. SignalR → PlanGenerated → wszyscy widzą wyniki
```

---

## Czy używamy tool callingu?

**Nie** — i to jest świadoma decyzja projektowa.

### Co to jest tool calling

W tool callingu (function calling) LLM sam decyduje kiedy i jak wywołać zewnętrzne narzędzia:

```
Prompt → LLM → "chcę wywołać search_attractions(city='Kraków')"
             → system wywołuje funkcję
             → wynik wraca do LLM
             → LLM kontynuuje rozumowanie
             → odpowiedź końcowa
```

LLM orkiestruje cały przepływ — to prawdziwa pętla agentyczna (observe → think → act → observe).

### Co robimy zamiast tego

Używamy **LLM-as-a-function** — prostszego wzorca:

```
Dane (Overpass) → Prompt → LLM → JSON string → ręczny parse → baza
```

LLM dostaje prompt i odpowiada **raz**. Nie decyduje o kolejnych krokach, nie wywołuje narzędzi.

### Dlaczego nie tool calling?

| Powód | Wyjaśnienie |
|---|---|
| **Deterministyczny pipeline** | Kolejność kroków jest zawsze ta sama: Overpass → LLM → zapis. LLM nie musi o niczym decydować. |
| **Kompatybilność z PSNC** | Endpoint PSNC nie jest oficjalnym OpenAI API — nie mamy gwarancji pełnej obsługi function calling. |
| **Przewidywalność** | Tool calling wprowadza niedeterminizm. Tu LLM ma jedno zadanie: zwróć poprawny JSON. |
| **Prostota** | Dwóch agentów, dwa prompty, dwa wywołania HTTP. Zero pętli, zero retry logic na poziomie LLM. |

### Kiedy warto by dodać tool calling?

Gdyby LLM miał **samodzielnie decydować** np.:
- czy szukać w Overpass czy w innej bazie
- ile razy doprecyzować wyniki
- czy dodać atrakcje z sąsiednich miast gdy lokalnych jest za mało

Wtedy tool calling miałoby sens. W obecnym MVP zakres jest jasno określony, więc prosty pipeline wystarcza.

---

## Ograniczenia (MVP)

| Parametr | Wartość |
|---|---|
| Maks. uczestników | 20 |
| Maks. dni wycieczki | 7 |
| Maks. atrakcji do głosowania | 20 |
| Autentykacja | Anonimowa — link z kodem dołączenia |
| Konflikty głosów | Większość: TAK > NIE = atrakcja wchodzi do planu |
| Wykluczenia kategorii | Osobiste (tylko dla danego uczestnika) |
| Routing między punktami | Markery na mapie, brak wyznaczonej trasy |

---

## Uruchomienie lokalne

Aplikacja używa **Caddy** jako gateway — jeden port obsługuje i frontend, i backend.

### Wymagania wstępne
```bash
brew install caddy   # jednorazowo
```

### Uruchomienie (3 terminale)

```bash
# Terminal 1 — Backend
cd backend/AlignRoute.Api
export LLM_API_KEY="twój-klucz-psnc"
dotnet run
# → http://localhost:5038 (backend tylko, nie otwieraj bezpośrednio)

# Terminal 2 — Caddy Gateway
cd AlignRouteProj          # katalog główny (tam gdzie Caddyfile)
caddy run
# → http://localhost:8080  ← tutaj otwierasz aplikację

# (Terminal 3 opcjonalnie — hot-reload Caddy config bez restartu)
# caddy reload
```

> **Uwaga:** Otwieraj aplikację na `http://localhost:8080`, nie na `localhost:5038`.
> Caddy automatycznie proxy-uje `/api/*` i `/hub` do backendu.

## Udostępnianie znajomym (ngrok)

Wystarczy **jeden** tunel — Caddy obsługuje wszystko na jednym porcie:

```bash
# Jeden tunel zamiast dwóch
ngrok http 8080   # → https://abc123.ngrok-free.app

# frontend/config.js zostaje pusty — nie trzeba niczego wpisywać.
# Caddy przekieruje /api/* i /hub do lokalnego backendu automatycznie.
```

---

## Zespół

Kamila Wachulec · Mateusz Rutkowski · Konrad Typa · Ziemowit Jakuczun

*Projekt akademicki — kurs „Tworzenie agentów AI w środowisku .NET", 2026*
