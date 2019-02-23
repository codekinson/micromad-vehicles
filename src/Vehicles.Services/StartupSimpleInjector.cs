using Codekinson.Microservices.HostedServices;
using GreenPipes;
using Marten;
using MassTransit;
using MassTransit.ExtensionsDependencyInjectionIntegration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.EntityFrameworkCore.Design.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SimpleInjector;
using SimpleInjector.Integration.AspNetCore.Mvc;
using SimpleInjector.Lifestyles;
using Vehicles.RestResources.v1;
using Vehicles.Services.Infrastructure.Consumers;
using Vehicles.Services.Models.Configuration;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace Vehicles.Services
{
    public class StartupSimpleInjector
    {
        private readonly Container _container = new Container();
        
        public StartupSimpleInjector(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            
            services.AddMassTransit(x =>
            {
                x.AddConsumer<VehicleConsumer>();
            });

            services.Configure<DatabaseConfig>(Configuration.GetSection("DatabaseConfig"));
            services.Configure<MessageQueueConfig>(Configuration.GetSection("MessageQueueConfig"));
            
            IntegrateSimpleInjector(services, _container);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            ConfigureContainer(app, _container);
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
            
            _container.Verify();
        }
        
        protected virtual void ConfigureContainer(IApplicationBuilder app, Container container) {
            // Add application presentation components:
            container.RegisterMvcControllers(app);
            container.RegisterMvcViewComponents(app);

            // Add application services. For instance:
            container.Register<IDocumentStore>(() =>
            {
                var config = container.GetInstance<IOptions<DatabaseConfig>>();
                
                return DocumentStore.For(storeOptions =>
                {
                    var connectionString = config.Value.VehiclesDatabaseConnectionString;
                    storeOptions.Connection(connectionString);
                    
                    storeOptions.CreateDatabasesForTenants(c =>
                    {
                        // Specify a db to which to connect in case database needs to be created.
                        // If not specified, defaults to 'postgres' on the connection for a tenant.
                        c.ForTenant()
                            .CheckAgainstPgDatabase()
                            .WithOwner("postgres")
                            .WithEncoding("UTF-8")
                            .ConnectionLimit(-1)
                            .OnDatabaseCreated(_ => {});
                    });
                });
            }, Lifestyle.Singleton);
            
            container.Register<VehicleConsumer>(Lifestyle.Scoped);
            
            container.Register(() => Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                var config = container.GetInstance<IOptions<MessageQueueConfig>>();

                var host = cfg.Host(config.Value.Host, 5672, "/", h =>
                {
                    h.Username("guest");
                    h.Password("guest");
                });

                cfg.ReceiveEndpoint(host, "create-vehicle", e =>
                {
                    e.PrefetchCount = 16;
                    e.UseMessageRetry(x => x.Interval(2, 100));

                    EndpointConvention.Map<VehicleConsumer>(e.InputAddress);
                });
            }), Lifestyle.Singleton);

            container.Register<IPublishEndpoint>(container.GetInstance<IBusControl>, Lifestyle.Singleton);
            container.Register<ISendEndpointProvider>(container.GetInstance<IBusControl>, Lifestyle.Singleton);
            container.Register<IBus>(container.GetInstance<IBusControl>, Lifestyle.Singleton);
            container.Register(() => container.GetInstance<IBus>().CreateRequestClient<Vehicle>(), Lifestyle.Scoped);

            container.Register<IHostedService, BusService>(Lifestyle.Singleton);
            
            // Allow Simple Injector to resolve services from ASP.NET Core.
            container.AutoCrossWireAspNetComponents(app);
        }
        
        private void IntegrateSimpleInjector(IServiceCollection services, Container container) {
            container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddSingleton<IControllerActivator>(
                new SimpleInjectorControllerActivator(container));
            services.AddSingleton<IViewComponentActivator>(
                new SimpleInjectorViewComponentActivator(container));

            services.EnableSimpleInjectorCrossWiring(container);
            services.UseSimpleInjectorAspNetRequestScoping(container);
        }
    }
}
