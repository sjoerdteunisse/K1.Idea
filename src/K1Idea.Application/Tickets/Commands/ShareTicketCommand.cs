using FluentValidation;
using K1Idea.Application.Common.Exceptions;
using K1Idea.Application.Common.Tenancy;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Enums;
using K1Idea.Domain.Interfaces;
using MediatR;

namespace K1Idea.Application.Tickets.Commands;

public sealed record ShareTicketCommand(
    Guid TicketId,
    IReadOnlyList<Guid> BusinessUnitIds) : IRequest<bool>;

public sealed class ShareTicketCommandValidator : AbstractValidator<ShareTicketCommand>
{
    public ShareTicketCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.BusinessUnitIds).NotEmpty();
    }
}

public sealed class ShareTicketCommandHandler(
    IUnitOfWork uow,
    ITicketRepository tickets,
    IBusinessUnitRepository bus,
    IOrgRepository orgs,
    TenantContext tenantCtx,
    OrgContext orgCtx,
    IClock clock) : IRequestHandler<ShareTicketCommand, bool>
{
    public async Task<bool> Handle(ShareTicketCommand request, CancellationToken ct)
    {
        var orgUser = await orgs.GetOrgUserAsync(orgCtx.OrgId, orgCtx.UserId, ct).ConfigureAwait(false)
            ?? throw new UnauthorizedException("Not a member of this organization.");

        var visibleBuIds = await bus.GetBusinessUnitIdsForUserAsync(orgCtx.OrgId, orgCtx.UserId, ct).ConfigureAwait(false);
        var ticket = await tickets.GetByIdScopedAsync(
            tenantCtx.TenantId, orgCtx.OrgId, request.TicketId, visibleBuIds, ct).ConfigureAwait(false)
            ?? throw new NotFoundException($"Ticket '{request.TicketId}' not found.");

        // Members can only share tickets they own
        if (orgUser.Role == UserRole.Member && ticket.OwnerBusinessUnitId != visibleBuIds.FirstOrDefault())
            throw new ForbiddenException("Members can only share tickets owned by their Business Unit.");

        await uow.BeginAsync(ct).ConfigureAwait(false);
        try
        {
            foreach (var buId in request.BusinessUnitIds)
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
            return true;
        }
        catch
        {
            await uow.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }
}
