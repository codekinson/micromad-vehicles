using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Vehicles.IntegrationTests.TestHelpers
{
    public class CleanDatabaseWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint>
        where TEntryPoint : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IHostedService, ConfigureMartenForTestsHostedService>();
            });
        }
    }
}