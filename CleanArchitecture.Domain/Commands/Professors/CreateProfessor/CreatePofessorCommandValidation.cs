using CleanArchitecture.Domain.Constants;
using CleanArchitecture.Domain.Errors;
using FluentValidation;

namespace CleanArchitecture.Domain.Commands.Professors.CreateProfessor;

public sealed class CreateProfessorCommandValidation : AbstractValidator<CreateProfessorCommand>
{
    public CreateProfessorCommandValidation()
    {
        AddRuleForId();
    }

    private void AddRuleForId()
    {
        RuleFor(cmd => cmd.AggregateId)
            .NotEmpty()
            .WithErrorCode(DomainErrorCodes.Professor.EmptyId)
            .WithMessage("Professor id may not be empty");
    }

   
}