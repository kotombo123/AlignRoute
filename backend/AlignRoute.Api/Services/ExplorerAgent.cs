using AlignRoute.Api.Models;

namespace AlignRoute.Api.Services;

public class ExplorerAgent
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public ExplorerAgent(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task<List<Attraction>> GenerateAttractionsAsync(int sessionId, string destination, string language)
    {
        // TODO (KAMILA): Integracja z gpt-oss_120b + tool calling
        throw new NotImplementedException();
    }
}