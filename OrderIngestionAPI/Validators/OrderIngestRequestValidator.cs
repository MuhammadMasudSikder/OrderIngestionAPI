
using Application.DTOs;
using FluentValidation;

namespace OrderIngestionAPI.Validators
{
    public class OrderIngestRequestValidator : AbstractValidator<CreateOrderRequest>
    {
        public OrderIngestRequestValidator()
        {
            RuleFor(x => x.RequestId)
            .NotEmpty()
            .WithErrorCode("ERR_REQUESTID_REQUIRED")
            .WithMessage("RequestId is required.");

            RuleFor(x => x.Customer.Email)
                .NotEmpty().WithErrorCode("ERR_EMAIL_REQUIRED")
                .WithMessage("CustomerEmail is required.")
                .EmailAddress().WithErrorCode("ERR_EMAIL_INVALID")
                .WithMessage("Invalid customer email format.");

            RuleForEach(x => x.Items).ChildRules(item =>
            {
                item.RuleFor(i => i.ProductName)
                    .NotEmpty()
                    .WithErrorCode("ERR_PRODUCT_NAME_REQUIRED")
                    .WithMessage("ProductName is required.");

                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0)
                    .WithErrorCode("ERR_INVALID_QUANTITY")
                    .WithMessage("Quantity must be greater than zero.");

                item.RuleFor(i => i.UnitPrice)
                    .GreaterThan(0)
                    .WithErrorCode("ERR_INVALID_PRICE")
                    .WithMessage("Price must be greater than zero.");
            });
        }
    }

}
