using Serilog;
using System.Net;
using System.Text.Json;
using FluentValidation;
using FluentValidation.Results;
using OrderIngestionAPI.Validators;

namespace OrderIngestionAPI.Middleware
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        public ErrorHandlingMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context); // Call the next middleware
            }
            catch (ValidationException ex)
            {
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                context.Response.ContentType = "application/json";

                var response = new ValidationErrorResponse();

                foreach (var error in ex.Errors)
                {
                    response.Errors.Add(new ErrorDetail
                    {
                        Code = error.ErrorCode,
                        Message = error.ErrorMessage
                    });
                }

                await context.Response.WriteAsync(JsonSerializer.Serialize(response));
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsync("Unexpected error occurred.");
            }
        }
    }

}
