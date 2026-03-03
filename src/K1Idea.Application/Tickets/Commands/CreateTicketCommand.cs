using FluentValidation;
using K1Idea.Application.Common.Exceptions;
using K1Idea.Application.Common.Tenancy;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Enums;
using K1Idea.Domain.Interfaces;
using MediatR;

namespace K1Idea.Application.Tickets.Commands;

public sealed record CreateTicketCommand(
    string Title,
    string? Description,
    TicketType Type,
    TicketPriority Priority,
    TicketStatus Status,
    Guid? ParentId,
    Guid? AssigneeId,
    Guid OwnerBusinessUnitId,
    IReadOnlyList<Guid> ShareWithBusinessUnitIds) : IRequest<Ticket>;

public sealed class CreateTicketCommandValidator : AbstractValidator<CreateTicketCommand>
{
    public CreateTicketCommandValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.OwnerBusinessUnitId).NotEmpty();
    }
}

public sealed class CreateTicketCommandHandler(
    IUnitOfWork uow,
    ITicketRepository tickets,
    IBusinessUnitRepository bus,
    TenantContext tenantCtx,
    OrgContext orgCtx,
    IClock clock) : IRequestHandler<CreateTicketCommand, Ticket>
{
    public async Task<Ticket> Handle(CreateTicketCommand request, CancellationToken ct)
    {
        var ownerBu = await bus.GetByIdAsync(tenantCtx.TenantId, orgCtx.OrgId, request.OwnerBusinessUnitId, ct).ConfigureAwait(false)
            ?? throw new NotFoundException($"Business unit '{request.OwnerBusinessUnitId}' not found.");

        await uow.BeginAsync(ct).ConfigureAwait(false);
        try
        {
            var ticket = new Ticket
            {
                Id = Guid.NewGuid(),
                TenantId = tenantCtx.TenantId,
                OrgId = orgCtx.OrgId,
                OwnerBusinessUnitId = request.OwnerBusinessUnitId,
                Title = request.Title,
                Description = request.Description,
                ReporterId = orgCtx.UserId,
                AssigneeId = request.AssigneeId,
                Type = request.Type,
                Status = request.Status,
                Priority = request.Priority,
                ParentId = request.ParentId,
                CreatedAt = clock.UtcNow,
                UpdatedAt = clock.UtcNow,
            };

            await tickets.InsertAsync(ticket, ct).ConfigureAwait(false);

            // Always share to owner BU
            var buIds = new HashSet<Guid>(request.ShareWithBusinessUnitIds) { request.OwnerBusinessUnitId };
            foreach (var buId in buIds)
            {
                await tickets.InsertTicketBusinessUnitAsync(new TicketBusinessUnit
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantCtx.TenantId,
                    OrgId = orgCtx.OrgId,
                    TicketId = ticket.Id,
                    BusinessUnitId = buId,
                    CreatedAt = clock.UtcNow,
                }, ct).ConfigureAwait(false);
            }

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
