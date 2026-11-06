namespace AlignRoute.Api.Dtos;

public sealed record UpdateExclusionsRequest(
    List<string> ExcludedCategories
);
