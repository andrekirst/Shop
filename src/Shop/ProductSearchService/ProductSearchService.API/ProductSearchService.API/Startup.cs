using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using Elasticsearch.Net;
using FluentTimeSpan;
using Microsoft.Extensions.HealthChecks;
using Microsoft.Extensions.Logging;
using ProductSearchService.API.Checks;
using Serilog;
using ProductSearchService.API.Repositories;
using ProductSearchService.API.Commands;
using ProductSearchService.API.Model;
using ProductSearchService.API.Events;
using ProductSearchService.API.Messaging;
using Serilog.Formatting.Json;
using Serilog.Sinks.RabbitMQ;
using Serilog.Sinks.RabbitMQ.Sinks.RabbitMQ;
using ProductSearchService.API.Caching;

namespace ProductSearchService.API
{
    public class Startup
    {
        private ILogger<Startup> Logger { get; }

        public Startup(ILogger<Startup> logger, IConfiguration configuration)
        {
            Logger = logger;
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var rabbitMessageQueueConfigSection = Configuration.GetSection(key: "RabbitMQ");

            var rabbitMqSerilogConfiguration = new RabbitMQConfiguration
            {
                Hostname = rabbitMessageQueueConfigSection[key: "Hostname"],
                Username = rabbitMessageQueueConfigSection[key: "Username"],
                Password = rabbitMessageQueueConfigSection[key: "Password"],
                Exchange = "ServiceLogging",
                ExchangeType = "fanout",
                DeliveryMode = RabbitMQDeliveryMode.Durable,
                RouteKey = "Logs",
                Port = 5672
            };

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration: Configuration)
                .Enrich.WithMachineName()
                .Enrich.FromLogContext()
                .Enrich.WithEnvironmentUserName()
                .WriteTo.RabbitMQ(
                    rabbitMqConfiguration: rabbitMqSerilogConfiguration,
                    formatter: new JsonFormatter())
                .CreateLogger();

            string connectionString = ConnectionString;
            Logger.LogDebug(message: $"ConnectionString: {connectionString}");

            services.AddTransient<IMessageSerializer, JsonMessageSerializer>();

            services.AddTransient<IMessagePublisher>(implementationFactory: sp => new RabbitMessageQueueMessagePublisher(
                hostname: rabbitMessageQueueConfigSection[key: "Hostname"],
                username: rabbitMessageQueueConfigSection[key: "Username"],
                password: rabbitMessageQueueConfigSection[key: "Password"],
                exchange: "SearchLog",
                messageSerializer: sp.GetService<IMessageSerializer>(),
                logger: sp.GetService<ILogger<RabbitMessageQueueMessagePublisher>>()));

            services
                .AddMvc()
                .AddNewtonsoftJson();

            var elasticNode = new Uri(uriString: ConnectionString);
            var elasticConfiguration = new ConnectionConfiguration(uri: elasticNode)
                .RequestTimeout(timeout: 2.Minutes());
            var client = new ElasticLowLevelClient(settings: elasticConfiguration);

            services.AddTransient<IElasticLowLevelClient, ElasticLowLevelClient>();
            services.AddTransient<IProductsRepository, ProductsRepository>(implementationFactory: sp =>
                new ProductsRepository(
                    logger: sp.GetService<ILogger<ProductsRepository>>(),
                    client: client));

            services.AddTransient(typeof(ICache<>), typeof(RedisCache<>));
            services.AddTransient<IRedisCacheSettings, RedisCacheSettings>();

            services.AddHealthChecks(checks: checks =>
            {
                checks.AddCheck(name: "AlwaysAvailable", check: () => new AlwaysAvailableCheck());
                checks.WithDefaultCacheDuration(duration: 3.Seconds());
                checks.WithPartialSuccessStatus(partiallyHealthyStatus: CheckStatus.Healthy);
            });
        }

        private string ConnectionString => Configuration.GetConnectionString(name: "ProductSearchConnectionString");

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime)
        {
            app.UseMvc();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            SetupAutoMapper();

            //app.UseSwagger();

            //app.UseSwaggerUI(setupAction: c =>
            //{
            //    c.SwaggerEndpoint(
            //        url: "/swagger/v1/swagger.json",
            //        name: "ProductSearchService.API - v1");
            //    c.DisplayOperationId();
            //    c.DisplayRequestDuration();
            //});

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting(configure: routes =>
            {
                routes.MapControllers();
            });

            app.UseAuthorization();
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
