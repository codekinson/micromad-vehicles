using System;
using System.Threading;
using System.Threading.Tasks;
using Marten;
using Microsoft.Extensions.Hosting;
using Vehicles.RestResources.v1;

namespace Vehicles.IntegrationTests.TestHelpers
{
    public class ConfigureMartenForTestsHostedService : IHostedService
    {
        private readonly IDocumentStore _documentStore;

        public ConfigureMartenForTestsHostedService(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _documentStore.Advanced.Clean.DeleteAllDocuments();
            _documentStore.BulkInsert(new []
            {
                CreateNewVehicle("Volkswagen", "Golf", 15000m),
                CreateNewVehicle("Audi", "A3", 18000m),
                CreateNewVehicle("Skoda", "Superb", 23000m)
            });
            
            return Task.CompletedTask;
        }

        private Vehicle CreateNewVehicle(string make, string model, decimal price)
        {
            return new Vehicle
            {
                Id = Guid.NewGuid(),
                Make = make,
                Model = model,
                Price = price
            };
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}