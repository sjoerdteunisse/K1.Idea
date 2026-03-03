using FluentValidation;
using K1Idea.Application.Common.Exceptions;
using K1Idea.Application.Common.Tenancy;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Enums;
using K1Idea.Domain.Interfaces;
using MediatR;

namespace K1Idea.Application.Tickets.Commands;

public sealed record UpdateTicketCommand(
    Guid TicketId,
    string? Title,
    string? Description,
    TicketStatus? Status,
    TicketPriority? Priority,
    Guid? AssigneeId) : IRequest<Ticket>;

public sealed class UpdateTicketCommandValidator : AbstractValidator<UpdateTicketCommand>
{
    public UpdateTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Title).MaximumLength(500).When(x => x.Title is not null);
    }
}

public sealed class UpdateTicketCommandHandler(
    IUnitOfWork uow,
    ITicketRepository tickets,
    IBusinessUnitRepository bus,
    TenantContext tenantCtx,
    OrgContext orgCtx,
    IClock clock) : IRequestHandler<UpdateTicketCommand, Ticket>
{
    public async Task<Ticket> Handle(UpdateTicketCommand request, CancellationToken ct)
    {
        var visibleBuIds = await bus.GetBusinessUnitIdsForUserAsync(orgCtx.OrgId, orgCtx.UserId, ct).ConfigureAwait(false);
        var ticket = await tickets.GetByIdScopedAsync(
            tenantCtx.TenantId, orgCtx.OrgId, request.TicketId, visibleBuIds, ct).ConfigureAwait(false)
            ?? throw new NotFoundException($"Ticket '{request.TicketId}' not found.");

        await uow.BeginAsync(ct).ConfigureAwait(false);
        try
        {
            if (request.Title is not null) ticket.Title = request.Title;
            if (request.Description is not null) ticket.Description = request.Description;
            if (request.Status.HasValue) ticket.Status = request.Status.Value;
            if (request.Priority.HasValue) ticket.Priority = request.Priority.Value;
            if (request.AssigneeId.HasValue) ticket.AssigneeId = request.AssigneeId.Value;
            ticket.UpdatedAt = clock.UtcNow;

            await tickets.UpdateAsync(ticket, ct).ConfigureAwait(false);
            await uow.CommitAsync(ct).ConfigureAwait(false);
            return ticket;
        }
        catch
        {
            await uow.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }
}
