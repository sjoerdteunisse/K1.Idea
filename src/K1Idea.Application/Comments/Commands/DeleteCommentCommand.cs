using FluentValidation;
using K1Idea.Application.Common.Exceptions;
using K1Idea.Application.Common.Tenancy;
using K1Idea.Domain.Enums;
using K1Idea.Domain.Interfaces;
using MediatR;

namespace K1Idea.Application.Comments.Commands;

public sealed record DeleteCommentCommand(Guid CommentId) : IRequest<bool>;

public sealed class DeleteCommentCommandValidator : AbstractValidator<DeleteCommentCommand>
{
    public DeleteCommentCommandValidator()
    {
        RuleFor(x => x.CommentId).NotEmpty();
    }
}

public sealed class DeleteCommentCommandHandler(
    IUnitOfWork uow,
    ICommentRepository comments,
    IOrgRepository orgs,
    TenantContext tenantCtx,
    OrgContext orgCtx) : IRequestHandler<DeleteCommentCommand, bool>
{
    public async Task<bool> Handle(DeleteCommentCommand request, CancellationToken ct)
    {
        var comment = await comments.GetByIdAsync(tenantCtx.TenantId, orgCtx.OrgId, request.CommentId, ct).ConfigureAwait(false)
            ?? throw new NotFoundException($"Comment '{request.CommentId}' not found.");

        var orgUser = await orgs.GetOrgUserAsync(orgCtx.OrgId, orgCtx.UserId, ct).ConfigureAwait(false)
            ?? throw new UnauthorizedException("Not a member of this organization.");

        var isOwn = comment.AuthorId == orgCtx.UserId;
        var isAdmin = orgUser.Role == UserRole.Admin;

        if (!isOwn && !isAdmin)
            throw new ForbiddenException("You may only delete your own comments.");

        await uow.BeginAsync(ct).ConfigureAwait(false);
        try
        {
            await comments.DeleteAsync(tenantCtx.TenantId, orgCtx.OrgId, request.CommentId, ct).ConfigureAwait(false);
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
