namespace K1Idea.Application.Tickets.DTOs;

public sealed record TicketSort(string Field = "created_at", string Direction = "DESC");
