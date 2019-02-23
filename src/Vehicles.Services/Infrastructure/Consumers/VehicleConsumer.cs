using System.Threading.Tasks;
using Marten;
using MassTransit;
using Vehicles.RestResources.v1;

namespace Vehicles.Services.Infrastructure.Consumers
{
    public class VehicleConsumer : IConsumer<Vehicle>
    {
        private readonly IDocumentStore _documentStore;

        public VehicleConsumer(IDocumentStore documentStore)
        {
            _documentStore = documentStore;
        }
        
        public async Task Consume(ConsumeContext<Vehicle> context)
        {
            using (var session = _documentStore.LightweightSession())
            {
                var vehicle = context.Message;
                session.Store(vehicle);
                await session.SaveChangesAsync();

                await context.RespondAsync(vehicle);
            }
        }
    }
}