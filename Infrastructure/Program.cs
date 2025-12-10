using Domain.Interfaces;
using Infrastructure.Messaging.Consumers;
using Infrastructure.Repositories;
using Infrastructure.Services;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Data;

namespace OrderIngestionAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            builder.Host.UseSerilog((ctx, lc) => lc.WriteTo.Console());


            // Get the connection string from appsettings
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

            // Register IDbConnection as scoped (per-request lifetime)
            builder.Services.AddScoped<IDbConnection>(sp => new SqlConnection(connectionString));

            builder.Services.AddScoped<IOrderRepository, OrderRepository>();
            builder.Services.AddScoped<ILogisticsGateway, MockLogisticsGateway>();

            builder.Services.AddMassTransit(x =>
            {
                x.AddConsumer<OrderProcessorConsumer>(cfg =>
                {
                    cfg.UseMessageRetry(r => r.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
                    cfg.UseInMemoryOutbox();
                });

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", h =>
                    {
                        h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
                        h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
                    });

                    cfg.ReceiveEndpoint("order-ingest-queue", ep =>
                    {
                        ep.ConfigureConsumer<OrderProcessorConsumer>(context);
                    });
                });
            });

            var app = builder.Build();



            app.Run();
        }
    }
}
