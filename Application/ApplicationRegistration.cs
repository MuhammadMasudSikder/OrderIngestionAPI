using Application.Commands.Orders;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Application
{
    public static class ApplicationRegistration
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {

            //services.AddMediatR(cfg =>
            //cfg.RegisterServicesFromAssembly(typeof(IngestOrderCommand).Assembly));

            return services;


        }
    }
}
