using System.ComponentModel.DataAnnotations;

namespace AlignRoute.Api.Dtos;

public sealed record CastVoteRequest(
    [Required] Guid ParticipantId,
    [Required] Guid AttractionId,
    bool IsPositive
);
