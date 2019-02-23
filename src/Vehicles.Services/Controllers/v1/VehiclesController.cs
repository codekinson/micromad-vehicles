using System;
using System.Threading;
using System.Threading.Tasks;
using Marten;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Vehicles.RestResources.v1;

namespace Vehicles.Services.Controllers.v1
{
    [Route("v1/[controller]")]
    public class VehiclesController : Controller
    {
        private readonly IDocumentStore _documentStore;
        private readonly IRequestClient<Vehicle> _requestClient;

        public VehiclesController(IDocumentStore documentStore, IRequestClient<Vehicle> requestClient)
        {
            _documentStore = documentStore;
            _requestClient = requestClient;
        }
        
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using (var session = _documentStore.QuerySession())
            {
                var vehicles = await session.Query<Vehicle>().ToListAsync();
                return Ok(vehicles);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get([FromRoute]Guid id, [FromQuery]Request request, CancellationToken cancellationToken)
        {
            using (var session = _documentStore.QuerySession())
            {
                var vehicle = await session.Query<Vehicle>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
                return Ok(vehicle);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody]Vehicle vehicle, CancellationToken cancellationToken)
        {
            var request = _requestClient.Create(vehicle, cancellationToken);

            var response = await request.GetResponse<Vehicle>();

            return CreatedAtAction(nameof(Get), new {id = response.Message.Id}, response.Message);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            using (var session = _documentStore.LightweightSession())
            {
                session.Delete<Vehicle>(id);
                await session.SaveChangesAsync(cancellationToken);
                return NoContent();
            }
        }
    }

    public class Request
    {
        public string ThisThing { get; set; }
        public string AnotherThing { get; set; }
    }
}