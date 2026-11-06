using System.ComponentModel.DataAnnotations;

namespace AlignRoute.Api.Dtos;

public sealed record JoinSessionRequest(
    [Required, StringLength(50, MinimumLength = 1)] string Nickname
);
