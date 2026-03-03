using FluentValidation;
using K1Idea.Application.Common.Exceptions;
using K1Idea.Application.Common.Tenancy;
using K1Idea.Domain.Entities;
using K1Idea.Domain.Interfaces;
using MediatR;

namespace K1Idea.Application.Comments.Commands;

public sealed record AddCommentCommand(Guid TicketId, string Body) : IRequest<Comment>;

public sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(x => x.TicketId).NotEmpty();
        RuleFor(x => x.Body).NotEmpty().MaximumLength(10000);
    }
}

public sealed class AddCommentCommandHandler(
    IUnitOfWork uow,
    ICommentRepository comments,
    ITicketRepository tickets,
    IBusinessUnitRepository bus,
    TenantContext tenantCtx,
    OrgContext orgCtx,
    IClock clock) : IRequestHandler<AddCommentCommand, Comment>
{
    public async Task<Comment> Handle(AddCommentCommand request, CancellationToken ct)
    {
        var visibleBuIds = await bus.GetBusinessUnitIdsForUserAsync(orgCtx.OrgId, orgCtx.UserId, ct).ConfigureAwait(false);
        var ticket = await tickets.GetByIdScopedAsync(
            tenantCtx.TenantId, orgCtx.OrgId, request.TicketId, visibleBuIds, ct).ConfigureAwait(false)
            ?? throw new NotFoundException($"Ticket '{request.TicketId}' not found.");

        await uow.BeginAsync(ct).ConfigureAwait(false);
        try
        {
            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                TenantId = tenantCtx.TenantId,
                OrgId = orgCtx.OrgId,
                TicketId = ticket.Id,
                AuthorId = orgCtx.UserId,
                Body = request.Body,
                CreatedAt = clock.UtcNow,
                UpdatedAt = clock.UtcNow,
            };

            await comments.InsertAsync(comment, ct).ConfigureAwait(false);
            await uow.CommitAsync(ct).ConfigureAwait(false);
            return comment;
        }
        catch
        {
            await uow.RollbackAsync(ct).ConfigureAwait(false);
            throw;
        }
    }
}
