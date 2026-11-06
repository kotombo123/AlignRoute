namespace AlignRoute.Api.Services;

public interface ILlmService
{
    Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken ct = default);
}
