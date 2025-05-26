using FluentValidation;
using ArtonitRESTAPI.Model;

namespace ArtonitRESTAPI.APIControllers
{
    public class IdentifierPostSeeValidator : AbstractValidator<IdentifirePostSee>
    {
        public IdentifierPostSeeValidator()
        {
            When(x => x != null, () =>
            {
                RuleFor(x => x.Id_cardtype)
                    .NotEmpty().WithMessage("Тип карты обязателен")
                    .InclusiveBetween(ForValidator.Conditions.CardTypeMin, ForValidator.Conditions.CardTypeMax).WithMessage(ForValidator.Text1);

                When(x => x.Id_cardtype == 1, () =>
                {
                    RuleFor(x => x.Id_card)
                        .NotEmpty()
                        .Matches(ForValidator.Conditions.IdCardType1Regex)
                        .WithMessage(ForValidator.IdCardType1Format);
                    //спросить, можно ли вынести это как-то в отдельный метод например
                    RuleFor(x => x.Id_pep)
                    .NotEmpty()
                    .GreaterThan(ForValidator.Conditions.ValidPep)
                    .WithMessage(ForValidator.IdPepPositive);
                });

                When(x => x.Id_cardtype == 2, () =>
                {
                    RuleFor(x => x.Id_card)
                        .NotEmpty()
                        .Matches(ForValidator.Conditions.IdCardType2Regex)
                        .WithMessage(ForValidator.IdCardType2Format);

                    RuleFor(x => x.Id_pep)
                    .NotEmpty()
                    .GreaterThan(ForValidator.Conditions.ValidPep)
                    .WithMessage(ForValidator.IdPepPositive);
                });

            });
        }
    }
}
