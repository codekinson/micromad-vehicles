using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Vehicles.IntegrationTests.TestHelpers;
using Vehicles.RestResources.v1;
using Xunit;

namespace Vehicles.IntegrationTests.Controllers.v1
{
    public class VehiclesControllerTests : IClassFixture<StartupWebApplicationFactoryFixture>
    {
        private readonly StartupWebApplicationFactoryFixture _appFactory;

        public VehiclesControllerTests(StartupWebApplicationFactoryFixture appFactory)
        {
            _appFactory = appFactory;
        }
        
        [Fact]
        public async Task GetAllVehicles_RetrievesAllVehicles()
        {
            var client = _appFactory.CreateClient();
            var response = await client.GetAsync("v1/vehicles");
            var contentBody = await response.Content.ReadAsAsync<IEnumerable<Vehicle>>();

            contentBody.Should().HaveCountGreaterOrEqualTo(3);
        }

        [Fact]
        public async Task CreateVehicle_CreatesVehicle()
        {
            var meh = _appFactory.CreateClient();
            var response = await meh.PostAsJsonAsync("v1/vehicles", new Vehicle
            {
                Make = "Volkswagen",
                Model = "Golf",
                Price = 16000m
            });
            response.StatusCode.Should().Be(HttpStatusCode.Created);
        }
    }
}