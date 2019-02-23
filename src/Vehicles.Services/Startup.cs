using System;
using Codekinson.Microservices.HostedServices;
using GreenPipes;
using Marten;
using MassTransit;
using MassTransit.AspNetCoreIntegration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using Vehicles.RestResources.v1;
using Vehicles.Services.Infrastructure.Consumers;
using Vehicles.Services.Infrastructure.Conventions;
using Vehicles.Services.Models.Configuration;
using IHostingEnvironment = Microsoft.AspNetCore.Hosting.IHostingEnvironment;

namespace Vehicles.Services
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            services
                .AddMvc(options =>
                {
                    options.Conventions.Add(new RouteTokenTransformerConvention(new KebabParameterTransformer()));
                    options.Conventions.Add(new KebabCaseParameterModelConvention());
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            
            services.AddScoped<VehicleConsumer>();

            // Register MassTransit
            services.AddMassTransit(
                provider => Bus.Factory.CreateUsingRabbitMq(cfg =>
                {
                    var rabbitMqHost = cfg.Host("localhost", "/", host => 
                    { 
                        host.Username("guest");
                        host.Password("guest");
                    });

                    cfg.ReceiveEndpoint(rabbitMqHost, "create-vehicle", ep =>
                    {
                        ep.PrefetchCount = 16;
                        ep.UseMessageRetry(x => x.Interval(2, 100));

                        ep.Consumer<VehicleConsumer>(provider);
                    });
                }),
                x =>
                {
                    x.AddConsumer<VehicleConsumer>();
                    
                    x.AddRequestClient<Vehicle>();    
                });
            
            services.AddSingleton(context => context.GetRequiredService<IBus>().CreateClientFactory());

            
            // Add application services. For instance:
            services.AddSingleton<IDocumentStore>(provider =>
            {
                var config = provider.GetRequiredService<IOptions<DatabaseConfig>>();
                
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
            });

            services.Configure<DatabaseConfig>(Configuration.GetSection("DatabaseConfig"));
            services.Configure<MessageQueueConfig>(Configuration.GetSection("MessageQueueConfig"));
            
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "My API", Version = "v1" });
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public virtual void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            });
            
            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
