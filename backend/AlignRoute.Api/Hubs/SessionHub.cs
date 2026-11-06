using Microsoft.AspNetCore.SignalR;

namespace AlignRoute.Api.Hubs;

public sealed class SessionHub : Hub
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

public static class SessionHubEvents
{
    public const string ParticipantJoined = "ParticipantJoined";
    public const string VoteUpdate = "VoteUpdate";
    public const string ParticipantFinished = "ParticipantFinished";
    public const string ExplorationComplete = "ExplorationComplete";
    public const string PlanGenerated = "PlanGenerated";
}
