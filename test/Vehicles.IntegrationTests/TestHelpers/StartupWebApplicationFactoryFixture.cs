using System;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Vehicles.Services;

namespace Vehicles.IntegrationTests.TestHelpers
{
    public class StartupWebApplicationFactoryFixture : WebApplicationFactoryFixture<Startup> {}
    
    public class WebApplicationFactoryFixture<TEntryPoint> : IDisposable
        where TEntryPoint : class
    {
        static WebApplicationFactoryFixture()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code)
                .CreateLogger();
        }
        
        private readonly WebApplicationFactory<TEntryPoint> _appFactory;

        public WebApplicationFactoryFixture()
        {
            _appFactory = new CleanDatabaseWebApplicationFactory<TEntryPoint>();
        }

        public HttpClient CreateClient()
        {
            return _appFactory.CreateClient();
        }

        public void Dispose()
        {
            _appFactory?.Dispose();
        }
    }
}