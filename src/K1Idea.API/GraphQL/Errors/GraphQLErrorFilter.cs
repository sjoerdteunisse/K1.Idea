using FluentValidation;
using HotChocolate;
using K1Idea.Application.Common.Exceptions;

namespace K1Idea.API.GraphQL.Errors;

public sealed class GraphQLErrorFilter : IErrorFilter
{
    public IError OnError(IError error)
    {
        return error.Exception switch
        {
            NotFoundException ex => error
                .WithMessage(ex.Message)
                .WithCode("NOT_FOUND")
                .RemoveException(),
            UnauthorizedException ex => error
                .WithMessage(ex.Message)
                .WithCode("AUTH_NOT_AUTHORIZED")
                .RemoveException(),
            ForbiddenException ex => error
                .WithMessage(ex.Message)
                .WithCode("FORBIDDEN")
                .RemoveException(),
            ValidationException ex => error
                .WithMessage(string.Join("; ", ex.Errors.Select(e => e.ErrorMessage)))
                .WithCode("VALIDATION_ERROR")
                .RemoveException(),
            _ => error
        };
    }
}
