# AlignRoute — Audyt Kodu, Martwy Kod i Rozbieżności

Dokument zawiera zestawienie martwego kodu, nieużywanych endpointów, wiszących wywołań API, nieaktywnych konfiguracji oraz rozbieżności architektonicznych zidentyfikowanych w projekcie **AlignRoute**.

---

## 1. Tabela podsumowująca

| Kategoria | Plik / Lokalizacja | Element | Status / Problem | Rekomendowane działanie |
|---|---|---|---|---|
| **API** | `frontend/js/api.js`<br>`backend/.../SessionEndpoints.cs` | `getCards()` / `GET /api/sessions/{id}/cards/{participantId}` | Nieużywany endpoint i metoda JS | Usunąć lub zintegrować z widokiem głosowania |
| **API** | `frontend/js/api.js`<br>`backend/.../SessionEndpoints.cs` | `getSessionStatus()` / `GET /api/sessions/{id}/status` | Nieużywany endpoint i metoda JS | Usunąć lub użyć do pollingu zapasowego |
| **API** | `frontend/js/voting.js`<br>`frontend/js/api.js` | `updateExclusions()` / `PUT /exclusions` | Frontend woła endpoint, który **nie istnieje** na backendzie | Dodać endpoint w backendzie lub usunąć request |
| **Frontend** | `frontend/js/utils.js` | `generateJoinCode()`, `debounce()`, `uuid()` | Wyeksportowane, nigdy niewywoływane | Usunąć lub przenieść do biblioteki pomocniczej |
| **Frontend** | `frontend/js/signalr-client.js` | `disconnect()`, `isConnected()`, `off()` | Wyeksportowane metody hub, nigdy niewywoływane | Zachować jako API publiczne klienta SignalR |
| **Frontend** | `frontend/js/map.js` | `clearLayers()` | Wyeksportowana metoda mapy, brak wywołań | Zachować lub usunąć |
| **Frontend** | `frontend/js/swipe.js` | `SWIPE_THRESHOLD` | Wyeksportowana stała używana tylko wewnętrznie | Ograniczyć zakres do pliku (usunąć z exportu) |
| **Frontend** | `frontend/js/i18n.js` | 8 kluczy tłumaczeniowych (PL i EN) | Klucze niezdefiniowane w HTML ani JS | Usunąć lub uzupełnić brakujące tłumaczenia w UI |
| **Backend** | `backend/.../Hubs/SessionHub.cs` | `LeaveSessionGroup()` | Metoda huba SignalR, której nikt nie woła | Zachować pod obsługę opuszczania pokoju |
| **Backend** | `backend/.../Program.cs` | `GET /api/health` | Healthcheck niepodpięty pod Caddy | Skonfigurować w Caddy lub zachować dla monitoringu |
| **Backend** | `backend/.../appsettings.json` | Sekcja `"SessionSettings"` | Wartości konfiguracyjne nieodczytywane w C# | Podpiąć pod `IOptions` / walidację reguł sesji |
| **Backend** | `backend/.../Models/Entities.cs` | `Vote.SessionId` | Pole w DB nigdy nieuzupełniane w kodzie | Przypisywać `SessionId` przy zapisie głosu |
| **Backend** | `backend/.../Models/Entities.cs` | `Attraction.Latitude/Longitude/ImageUrl` | Pola w DB zawsze mają wartość `null` | Wzbogacić prompt LLM o współrzędne / geokodowanie |
| **Repozytorium** | `.Rhistory` | Pusty plik w katalogu głównym | Pozostałość po sesji R | Dodać do `.gitignore` i usunąć |
| **Dokumentacja** | `HANDOFF.md` | `LlmService`, `OverpassService`, `PlannerAgent`, `Dtos/`, `Migrations/` | Pliki opisane w dokumencie, które fizycznie nie istnieją | Zaktualizować opis architektury |

---

## 2. Szczegółowy opis znalezisk

### 2.1. Martwe endpointy i metody API (Frontend ↔ Backend)

#### `GET /api/sessions/{id}/cards/{participantId}` oraz `api.getCards`
* **Lokalizacja backend:** `backend/AlignRoute.Api/Endpoints/SessionEndpoints.cs` (linie 76–82)
* **Lokalizacja frontend:** `frontend/js/api.js` (linie 106–111)
* **Opis:** Endpoint miał zwracać spersonalizowane karty dla uczestnika. W rzeczywistości frontend pobiera wszystkie atrakcje sesji przez `GET /api/sessions/{id}/attractions` (`api.getAttractions`) i filtruje wykluczenia lokalnie po stronie przeglądarki (`frontend/js/voting.js`). Endpoint na backendzie ani funkcja `api.getCards` w JS nie są nigdzie wywoływane.

#### `GET /api/sessions/{id}/status` oraz `api.getSessionStatus`
* **Lokalizacja backend:** `backend/AlignRoute.Api/Endpoints/SessionEndpoints.cs` (linie 61–74)
* **Lokalizacja frontend:** `frontend/js/api.js` (linie 87–89)
* **Opis:** Endpoint zwracający aktualny status sesji i listę uczestników. Frontend w całości opiera odświeżanie stanu sesji i uczestników na zdarzeniach SignalR (`ParticipantJoined`, `ExplorationComplete`, `PlanGenerated`), przez co ten endpoint wisi nieużywany.

#### Brakujący endpoint: `PUT /sessions/{sessionId}/participants/{participantId}/exclusions`
* **Lokalizacja frontend:** `frontend/js/voting.js` (linia 361) oraz `frontend/js/api.js` (linie 123–134)
* **Opis:** Gdy użytkownik klika tagi wykluczeń kategorii w UI, frontend wywołuje `window.api.updateExclusions(...)`. Backend **nie posiada** takiego endpointu w Minimal API. W trybie produkcyjnym zapytanie HTTP kończy się błędem `404 Not Found` (aplikacja nie wywala się tylko dlatego, że stan wykluczeń aktualizuje się w pamięci lokalnej JS).

---

### 2.2. Martwy kod na frontendzie (JavaScript)

#### `frontend/js/utils.js`
* `generateJoinCode()` (linia 116) – generowanie kodów sesji przeniesiono na backend w C# (`SessionEndpoints.GenerateJoinCode`). Funkcja na frontendzie jest martwa.
* `debounce()` (linia 131) – funkcja debounce przygotowana jako helper, brak jakichkolwiek wywołań w kodzie aplikacji.
* `uuid()` (linia 160) – generator UUID dla celów mockowych, nieużywany.

#### `frontend/js/signalr-client.js`
* `disconnect()` – zamykanie połączenia z hubem, nigdzie niepodpięte pod cykl życia aplikacji.
* `isConnected()` – sprawdzanie stanu połączenia, żaden komponent nie pyta o ten stan bezpośrednio.
* `off()` – wyrejestrowywanie handlerów eventów SignalR, nieużywane.

#### `frontend/js/map.js`
* `clearLayers()` (linia 128) – czyści warstwy z mapy Leaflet. Zdefiniowane i wyeksportowane w `window.mapUtils`, ale nigdzie niewywoływane (mapa jest inicjalizowana jednorazowo przy wejściu do wyników).

#### `frontend/js/swipe.js`
* `SWIPE_THRESHOLD` – próg przesunięcia karty w pikselach. Wyeksportowany w `window.swipe`, ale używany tylko wewnętrznie w `swipe.js`.

#### `frontend/js/i18n.js` (nieużywane klucze)
Następujące klucze językowe istnieją w słownikach `pl` i `en`, ale nie występują w `index.html` ani w plikach JS:
1. `error.session.not.found`
2. `nav.home`
3. `nav.new`
4. `results.duration`
5. `results.generating`
6. `results.share`
7. `voting.cards.of`
8. `voting.voted`

---

### 2.3. Martwy kod i braki w logice backendu (.NET)

#### `SessionHub.LeaveSessionGroup(string sessionId)`
* **Lokalizacja:** `backend/AlignRoute.Api/Hubs/SessionHub.cs` (linie 27–30)
* **Opis:** Metoda opuszczenia grupy SignalR. Frontend przy opuszczaniu widoków nie wywołuje `LeaveSessionGroup`.

#### `GET /api/health`
* **Lokalizacja:** `backend/AlignRoute.Api/Program.cs` (linia 32)
* **Opis:** Endpoint dodany z myślą o reverse proxy. Obecnie `Caddyfile` nie posiada skonfigurowanego aktywnego monitorowania stanu serwisu docelowego (health checks), więc endpoint nie jest odpytywany.

#### Sekcja `"SessionSettings"` w `appsettings.json`
* **Lokalizacja:** `backend/AlignRoute.Api/appsettings.json` (linie 18–22)
* **Zawartość:**
  ```json
  "SessionSettings": {
    "MaxParticipants": 20,
    "MaxAttractions": 20,
    "MaxDurationDays": 7
  }
  ```
* **Opis:** Wartości te nie są wstrzykiwane przez `IOptions<SessionSettings>` ani sprawdzane w `SessionEndpoints.cs`. Walidacja limitu uczestników czy maksymalnej liczby dni nie jest egzekwowana w C#.

#### Encja `Vote` i pole `SessionId`
* **Lokalizacja:** `backend/AlignRoute.Api/Models/Entities.cs` (linia 46)
* **Problem:** W `SessionEndpoints.cs` (linia 122) podczas zapisu głosu tworzony jest obiekt:
  ```csharp
  db.Votes.Add(new Vote
  {
      ParticipantId = req.ParticipantId,
      AttractionId = req.AttractionId,
      IsPositive = req.IsPositive
  });
  ```
  Pole `SessionId` nie jest uzupełniane i do bazy SQLite zawsze trafia domyślna wartość `0`.

#### Encja `Attraction`: `Latitude`, `Longitude`, `ImageUrl`
* **Lokalizacja:** `backend/AlignRoute.Api/Models/Entities.cs` (linie 37–39)
* **Problem:** Agent AI (`ExplorerAgent.cs`) generuje wyłącznie `Name`, `Description` oraz `Category`. Pola ze współrzędnymi geograficznymi i adresem grafiki są w bazie danych zawsze równe `null`.

---

### 2.4. Pliki śmieciowe w repozytorium

* **`.Rhistory`** – pusty plik tekstowy o rozmiarze 0 bajtów w katalogu głównym projektu. Warto dodać wpis `*.Rhistory` do `.gitignore`.
* **`frontend/.DS_Store`** – plik metadanych widoku katalogu macOS.

---

### 2.5. Rozbieżności w dokumentacji (`HANDOFF.md`)

W pliku `HANDOFF.md` opisano strukturę architektury, która częściowo nie odpowiada stanowi faktycznemu w kodzie:
* `Services/LlmService.cs` — nie istnieje (wywołania HTTP do PSNC są w całości w `ExplorerAgent.cs`).
* `Services/OverpassService.cs` — nie istnieje (projekt nie odpytuje OpenStreetMap Overpass API, atrakcje generuje bezpośrednio LLM).
* `Services/PlannerAgent.cs` — nie istnieje (endpoint `generate-plan` zwraca obecnie statyczny mock string w JSON).
* `Dtos/` — nie istnieje jako osobny folder (rekordy DTO są zadeklarowane bezpośrednio pod endpointami).
* `Migrations/` — nie istnieje jako folder (schemat bazy tworzy się w locie przez `Database.EnsureCreated()`).
