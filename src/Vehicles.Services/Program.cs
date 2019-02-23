using System;
using Codekinson.Microservices.Extensions;
using MassTransit;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Vehicles.Services.Models.Configuration;


namespace Vehicles.Services
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .CreateLogger();

            try
            {
                Log.Information("Starting web host.");
                CreateWebHostBuilder(args)
                    .Build()
                    .EnsureRabbitMqServerAvailable((services, factory) =>
                    {
                        var config = services.GetRequiredService<IOptions<MessageQueueConfig>>();
                        
                        var host = factory.CreateUsingRabbitMq(cfg => cfg.Host(config.Value.Host, 5672, "/", h =>
                        {
                            h.Username("guest");
                            h.Password("guest");
                        }));

                        return host;
                    })
                    .Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly!");
                return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseSerilog();
    }
}
