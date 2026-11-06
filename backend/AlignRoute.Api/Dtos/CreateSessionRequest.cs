using System.ComponentModel.DataAnnotations;

namespace AlignRoute.Api.Dtos;

public sealed record CreateSessionRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Destination,
    [Required, Range(1, 7)] int DurationDays,
    [Required, StringLength(50, MinimumLength = 1)] string OrganizerNickname,
    [RegularExpression("^(pl|en)$")] string Language = "en"
);
