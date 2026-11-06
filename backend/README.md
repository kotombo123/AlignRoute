# AlignRoute Backend

ASP.NET Core Minimal API backend for AlignRoute — a group travel planning app with AI-powered attraction discovery and itinerary generation.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQLite (bundled via EF Core)

## Setup

1. **Clone and navigate to the backend directory:**
   ```bash
   cd backend
   ```

2. **Configure the LLM API key** (required for AI features):
   
   Set the environment variable:
   ```bash
   export LLM_API_KEY="your-psnc-api-key"
   ```
   
   Or edit `AlignRoute.Api/appsettings.json` and set `LlmSettings:ApiKey`.

3. **Run the application:**
   ```bash
   cd AlignRoute.Api
   dotnet run
   ```
   
   The database is created and migrations are applied automatically on startup.

4. **Verify it is running:**
   ```bash
   curl http://localhost:5000/health
   ```

## API Endpoints

### Sessions
| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/sessions` | Create a new trip session |
| GET | `/api/sessions/{joinCode}` | Get session info by join code |
| POST | `/api/sessions/{joinCode}/join` | Join a session |
| GET | `/api/sessions/{sessionId}/status` | Get session status and participants |

### Attractions
| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/sessions/{sessionId}/explore` | Trigger AI attraction discovery |
| GET | `/api/sessions/{sessionId}/attractions` | List all attractions |

### Voting
| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/sessions/{sessionId}/cards/{participantId}` | Get unvoted cards |
| POST | `/api/sessions/{sessionId}/vote` | Cast a vote |
| PUT | `/api/sessions/{sessionId}/participants/{participantId}/exclusions` | Update excluded categories |
| PUT | `/api/sessions/{sessionId}/participants/{participantId}/done` | Mark voting complete |

### Itinerary
| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/sessions/{sessionId}/generate-plan` | Generate day-by-day itinerary |
| GET | `/api/sessions/{sessionId}/itinerary` | Get generated itinerary |

### Real-time (SignalR)
Connect to `/hubs/session` for live updates:
- `ParticipantJoined(nickname)`
- `VoteUpdate(attractionId, yesCount, noCount)`
- `ParticipantFinished(nickname)`
- `ExplorationComplete(attractionCount)`
- `PlanGenerated(itineraryId)`

## Configuration

Key settings in `appsettings.json`:

| Setting | Description | Default |
|---------|-------------|---------|
| `ConnectionStrings:DefaultConnection` | SQLite database path | `Data Source=alignroute.db` |
| `LlmSettings:BaseUrl` | PSNC LLM API base URL | `https://llm.hpc.psnc.pl` |
| `LlmSettings:Model` | LLM model name | `gpt-oss_120b` |
| `LlmSettings:ApiKey` | Bearer token (or use `LLM_API_KEY` env var) | _(empty)_ |
| `SessionSettings:MaxParticipants` | Max participants per session | `20` |
| `SessionSettings:MaxAttractions` | Max attractions per session | `20` |

## Project Structure

```
AlignRoute.Api/
  Program.cs              — App entry point, DI, middleware
  Data/AppDbContext.cs     — EF Core database context
  Models/                  — Entity models (Session, Participant, Attraction, Vote, Itinerary)
  Dtos/                    — Request/response DTOs
  Endpoints/               — Minimal API endpoint groups
  Services/                — Business logic (LLM, Overpass, Explorer, Planner agents)
  Hubs/SessionHub.cs       — SignalR hub for real-time updates
  Migrations/              — EF Core database migrations
```
