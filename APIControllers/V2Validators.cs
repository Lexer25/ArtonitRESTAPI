using ArtonitRESTAPI.DBControllers;
using ArtonitRESTAPI.Model;
using FluentValidation;
using OpenAPIArtonit.Model;

namespace ArtonitRESTAPI.APIControllers
{
    /// <summary>
    /// Валидатор тела запроса на добавление сотрудника в API v2.
    /// </summary>
    public class PersonV2PostSeeValidator : AbstractValidator<PersonV2PostSee>
    {
        /// <summary>
        /// Настраивает правила валидации.
        /// </summary>
        public PersonV2PostSeeValidator()
        {
            RuleFor(x => x.Surname)
                .NotEmpty().WithMessage("Заполнение Surname обязательно")
                .MaximumLength(50).WithMessage("Surname не должен превышать 50 символов");

            RuleFor(x => x.Name)
                .MaximumLength(50).WithMessage("Name не должен превышать 50 символов");

            RuleFor(x => x.Patronymic)
                .MaximumLength(50).WithMessage("Patronymic не должен превышать 50 символов");

            RuleFor(x => x.TabNum)
                .MaximumLength(TabnumResolver.MaxLength)
                .WithMessage($"tabnum не должен превышать {TabnumResolver.MaxLength} символов");
        }
    }

    /// <summary>
    /// Валидатор тела запроса на изменение сотрудника в API v2.
    /// </summary>
    public class PersonV2PatchValidator : AbstractValidator<PersonV2Patch>
    {
        /// <summary>
        /// Настраивает правила валидации.
        /// </summary>
        public PersonV2PatchValidator()
        {
            RuleFor(x => x.TabNum)
                .NotEmpty().WithMessage("Заполнение tabnum обязательно")
                .MaximumLength(TabnumResolver.MaxLength)
                .WithMessage($"tabnum не должен превышать {TabnumResolver.MaxLength} символов");

            RuleFor(x => x.id_db)
                .GreaterThan(0).WithMessage("id_db должен быть положительным числом")
                .When(x => x.id_db.HasValue);

            RuleFor(x => x.id_org)
                .GreaterThan(0).WithMessage("id_org должен быть положительным числом")
                .When(x => x.id_org.HasValue);
        }
    }

    /// <summary>
    /// Валидатор тела запроса на добавление категории доступа сотруднику в API v2.
    /// </summary>
    public class PersonV2AddAccessSeeValidator : AbstractValidator<PersonV2AddAccessSee>
    {
        /// <summary>
        /// Настраивает правила валидации.
        /// </summary>
        public PersonV2AddAccessSeeValidator()
        {
            RuleFor(x => x.TabNum)
                .NotEmpty().WithMessage("Заполнение tabnum обязательно")
                .MaximumLength(TabnumResolver.MaxLength)
                .WithMessage($"tabnum не должен превышать {TabnumResolver.MaxLength} символов");

            RuleFor(x => x.Id_accessname)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("Заполнение Id_accessname обязательно")
                .GreaterThan(0).WithMessage("Id_accessname должен быть положительным числом");
        }
    }

    /// <summary>
    /// Валидатор тела запроса на добавление идентификатора в API v2.
    /// </summary>
    public class IdentifierV2PostSeeValidator : AbstractValidator<IdentifierV2PostSee>
    {
        /// <summary>
        /// Настраивает правила валидации.
        /// </summary>
        public IdentifierV2PostSeeValidator()
        {
            RuleFor(x => x.TabNum)
                .NotEmpty().WithMessage("Заполнение tabnum обязательно")
                .MaximumLength(TabnumResolver.MaxLength)
                .WithMessage($"tabnum не должен превышать {TabnumResolver.MaxLength} символов");

            RuleFor(x => x.Id_cardtype)
                .InclusiveBetween(ForValidator.Conditions.CardTypeMin, ForValidator.Conditions.CardTypeMax)
                .WithMessage(ForValidator.Text1);

            When(x => x.Id_cardtype == 1, () =>
            {
                RuleFor(x => x.Id_card)
                    .NotEmpty().WithMessage("Заполнение Id_card обязательно")
                    .Matches(ForValidator.Conditions.IdCardType1Regex)
                    .WithMessage(ForValidator.IdCardType1Format);
            });

            When(x => x.Id_cardtype == 2, () =>
            {
                RuleFor(x => x.Id_card)
                    .NotEmpty().WithMessage("Заполнение Id_card обязательно")
                    .Matches(ForValidator.Conditions.IdCardType2Regex)
                    .WithMessage(ForValidator.IdCardType2Format);
            });
        }
    }
}
