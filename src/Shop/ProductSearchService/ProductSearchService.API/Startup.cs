using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
using AutoMapper;
using Elasticsearch.Net;
using FluentTimeSpan;
using Microsoft.Extensions.Logging;
using Serilog;
using ProductSearchService.API.Repositories;
using ProductSearchService.API.Commands;
using ProductSearchService.API.Model;
using ProductSearchService.API.Events;
using ProductSearchService.API.Messaging;
using BeatPulse;
using BeatPulse.UI;

namespace ProductSearchService.API
{
    public class Startup
    {
        private readonly ILogger<Startup> _logger;

        public Startup(ILogger<Startup> logger, IConfiguration configuration)
        {
            _logger = logger;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string connectionString = ConnectionString;
            _logger.LogDebug(message: $"ConnectionString: {connectionString}");

            services.AddTransient<IMessageSerializer, JsonMessageSerializer>();

            var configSection = Configuration.GetSection(key: "RabbitMQ");
            string hostname = configSection[key: "Hostname"];
            string username = configSection[key: "Username"];
            string password = configSection[key: "Password"];
            services.AddTransient<IMessagePublisher>(implementationFactory: sp => new RabbitMessageQueueMessagePublisher(
                hostname: hostname,
                username: username,
                password: password,
                exchange: "SearchLog",
                messageSerializer: sp.GetService<IMessageSerializer>(),
                logger: sp.GetService<ILogger<RabbitMessageQueueMessagePublisher>>()));

            services
                .AddMvc()
                .AddNewtonsoftJson();

            var node = new Uri(uriString: ConnectionString);
            var config = new ConnectionConfiguration(uri: node)
                .RequestTimeout(timeout: 2.Minutes());
            var client = new ElasticLowLevelClient(settings: config);

            services.AddTransient<IElasticLowLevelClient, ElasticLowLevelClient>();
            services.AddTransient<IProductsRepository, ProductsRepository>(implementationFactory: sp =>
                new ProductsRepository(
                    logger: sp.GetService<ILogger<ProductsRepository>>(),
                    client: client));

            services.AddSwaggerGen(setupAction: c =>
            {
                c.SwaggerDoc(name: "v1", info: new Info { Title = "ProductSearchService.API", Version = "v1" });
            });

            services.AddBeatPulse(setup =>
            {
                setup.AddElasticsearch(connectionString);
                setup.AddRabbitMQ($"amqp://{username}:{password}@{hostname}:15672/vhost");
                setup.AddDiskStorageLiveness(options: options =>
                {
                    //options.AddDrive()
                });
            });
        }

        private string ConnectionString => Configuration.GetConnectionString(name: "ProductSearchConnectionString");

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration: Configuration)
                .Enrich.WithMachineName()
                .CreateLogger();

            app.UseMvc();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            SetupAutoMapper();

            app.UseSwagger();

            app.UseSwaggerUI(setupAction: c =>
            {
                c.SwaggerEndpoint(url: "/swagger/v1/swagger.json", name: "ProductSearchServiceAPI - v1");
            });

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();

            app.UseRouting(configure: routes =>
            {
                routes.MapApplication();
            });

            app.UseAuthorization();

            app.UseBeatPulse(options =>
            {
                options
                    .ConfigurePath(path: "health")
                    .ConfigureTimeout(milliseconds: 1500)
                    .ConfigureDetailedOutput(
                        detailedOutput: true,
                        includeExceptionMessages: true);
            });
        }

        private void SetupAutoMapper()
        {
            Mapper.Initialize(config: config =>
            {
                config.CreateMap<SelectProductCommand, Product>();
                config
                    .CreateMap<Product, SelectProductCommand>()
                    .ForCtorParam(ctorParamName: "messageId", paramOptions: opt => opt.MapFrom(sourceMember: c => Guid.NewGuid()));
                config
                    .CreateMap<SelectProductCommand, ProductSelectedEvent>()
                    .ForCtorParam(ctorParamName: "messageId", paramOptions: opt => opt.MapFrom(sourceMember: c => Guid.NewGuid()));
            });
        }
    }
}
