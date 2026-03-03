using K1Idea.Domain.Entities;

namespace K1Idea.Application.Auth.DTOs;

public sealed record AuthPayload(string AccessToken, string RefreshToken, User User);
