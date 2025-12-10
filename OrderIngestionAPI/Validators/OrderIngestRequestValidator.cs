
using Application.DTOs;
using FluentValidation;

namespace OrderIngestionAPI.Validators
{
    public class OrderIngestRequestValidator : AbstractValidator<OrderIngestRequest>
    {
        public OrderIngestRequestValidator()
        {
            RuleFor(x => x.ExternalOrderId)
            .NotEmpty()
            .WithErrorCode("ERR_ORDERID_REQUIRED")
            .WithMessage("ExternalOrderId is required.");

            RuleFor(x => x.CustomerEmail)
                .NotEmpty().WithErrorCode("ERR_EMAIL_REQUIRED")
                .WithMessage("CustomerEmail is required.")
                .EmailAddress().WithErrorCode("ERR_EMAIL_INVALID")
                .WithMessage("Invalid customer email format.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ItemName)
                    .NotEmpty()
                    .WithErrorCode("ERR_ITEMNAME_REQUIRED")
                    .WithMessage("ItemName is required.");

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithErrorCode("ERR_INVALID_QUANTITY")
                    .WithMessage("Quantity must be greater than zero.");

                item.RuleFor(i => i.Price)
                    .GreaterThan(0)
                    .WithErrorCode("ERR_INVALID_PRICE")
                    .WithMessage("Price must be greater than zero.");
            });
        }
    }

}
