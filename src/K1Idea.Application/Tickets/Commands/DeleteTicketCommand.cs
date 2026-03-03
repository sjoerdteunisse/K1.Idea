using FluentValidation;
using K1Idea.Application.Common.Exceptions;
using K1Idea.Application.Common.Tenancy;
using K1Idea.Domain.Enums;
using K1Idea.Domain.Interfaces;
using MediatR;

namespace K1Idea.Application.Tickets.Commands;

public sealed record DeleteTicketCommand(Guid TicketId) : IRequest<bool>;

public sealed class DeleteTicketCommandValidator : AbstractValidator<DeleteTicketCommand>
{
    public DeleteTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
    }
}

public sealed class DeleteTicketCommandHandler(
    IUnitOfWork uow,
    ITicketRepository tickets,
    IBusinessUnitRepository bus,
    IOrgRepository orgs,
    TenantContext tenantCtx,
    OrgContext orgCtx,
    IClock clock) : IRequestHandler<DeleteTicketCommand, bool>
{
    public async Task<bool> Handle(DeleteTicketCommand request, CancellationToken ct)
    {
        var orgUser = await orgs.GetOrgUserAsync(orgCtx.OrgId, orgCtx.UserId, ct).ConfigureAwait(false)
            ?? throw new UnauthorizedException("Not a member of this organization.");

        if (orgUser.Role != UserRole.Admin)
            throw new ForbiddenException("Only Admins can delete tickets.");

        var visibleBuIds = await bus.GetBusinessUnitIdsForUserAsync(orgCtx.OrgId, orgCtx.UserId, ct).ConfigureAwait(false);
        var ticket = await tickets.GetByIdScopedAsync(
            tenantCtx.TenantId, orgCtx.OrgId, request.TicketId, visibleBuIds, ct).ConfigureAwait(false)
            ?? throw new NotFoundException($"Ticket '{request.TicketId}' not found.");

        await uow.BeginAsync(ct).ConfigureAwait(false);
        try
        {
            await tickets.SoftDeleteAsync(tenantCtx.TenantId, orgCtx.OrgId, ticket.Id, clock.UtcNow, ct).ConfigureAwait(false);
            await uow.CommitAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch
        {
            await uow.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }
}
