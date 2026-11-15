// SessionHub.cs — hub SignalR do komunikacji real-time między uczestnikami sesji.
//
// Frontend łączy się z /hub i po dołączeniu do sesji wywołuje:
//   connection.invoke("JoinSessionGroup", sessionId)
//
// Żeby rozesłać event do wszystkich w sesji, wstrzyknij IHubContext<SessionHub> do endpointu i użyj:
//   await hubContext.Clients.Group(sessionId.ToString()).SendAsync("NazwaEventu", dane, ct);
//
// Eventy których nasłuchuje frontend (nazwy muszą się zgadzać dokładnie):
//   "ParticipantJoined"    → { participantId, nickname }
//   "ExplorationComplete"  → (brak danych) — sygnał do pobrania listy atrakcji
//   "PlanGenerated"        → (brak danych lub id itinerary) — sygnał do pokazania wyników
//   "VoteUpdate"           → (opcjonalnie) — odświeżenie sidebaru głosowania
//   "ParticipantFinished"  → (opcjonalnie) — uczestnik skończył głosować

using Microsoft.AspNetCore.SignalR;

namespace AlignRoute.Api.Hubs;

public class SessionHub : Hub
{
    public async Task JoinSessionGroup(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
    }

    public async Task LeaveSessionGroup(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
    }
}
