using Credo.FileStorage.Application.Errors;
using FluentValidation;

namespace Credo.FileStorage.Application.Features.Todo.Commands.CreateTodo;

public class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator()
    {
        RuleFor(element => element.Name)
            .MaximumLength(10)
            .WithMessage(DomainErrors.Todo.NameIsTooLong.Message);
    }
}