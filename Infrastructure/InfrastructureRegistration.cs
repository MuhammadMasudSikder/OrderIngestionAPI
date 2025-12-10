using Application.interfaces;
using Domain.Interfaces;
using Humanizer.Configuration;
using Infrastructure.Messaging.Consumers;
using Infrastructure.Repositories;
using Infrastructure.Services;
using MassTransit;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public static class InfrastructureRegistration
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
        {
            // Ensure API passes configuration
            var connectionString = config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new Exception("DefaultConnection string is missing!");

            // Register IDbConnection as scoped (per-request lifetime)
            services.AddScoped<IDbConnection>(sp =>
                new SqlConnection(connectionString));

            Console.WriteLine(connectionString);
            Console.WriteLine(config["RabbitMQ:Host"]);
            // Repositories
            services.AddScoped<IOrderRepository, OrderRepository>();

            // External Services
            services.AddScoped<ILogisticsGateway, MockLogisticsGateway>();

            // Business Services
            services.AddScoped<IOrderService, OrderService>(); // fix

            // MassTransit
            services.AddMassTransit(x =>
            {
                x.AddConsumer<OrderProcessorConsumer>(cfg =>
                {
                    cfg.UseMessageRetry(r => r.Exponential(
                        5,
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(30),
                        TimeSpan.FromSeconds(2)
                    ));
                    cfg.UseInMemoryOutbox();
                });

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(config["RabbitMQ:Host"] ?? "localhost", h =>
                    {
                        h.Username(config["RabbitMQ:Username"] ?? "guest");
                        h.Password(config["RabbitMQ:Password"] ?? "guest");
                    });

                    cfg.ReceiveEndpoint("order-ingest-queue", ep =>
                    {
                        ep.ConfigureConsumer<OrderProcessorConsumer>(context);
                    });
                });
            });

            return services;
        }
    }

}
