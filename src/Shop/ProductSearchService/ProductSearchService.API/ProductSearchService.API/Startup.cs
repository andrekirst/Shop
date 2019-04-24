using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
using FluentTimeSpan;
using Microsoft.Extensions.HealthChecks;
using Microsoft.Extensions.Logging;
using ProductSearchService.API.Checks;
using ProductSearchService.API.Repositories;
using ProductSearchService.API.Commands;
using ProductSearchService.API.Model;
using ProductSearchService.API.Events;
using ProductSearchService.API.Messaging;
using ProductSearchService.API.Caching;
using Microsoft.Extensions.Hosting;
using ProductSearchService.API.EventHandlers;
using ProductSearchService.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Cors.Infrastructure;

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
            services.AddSingleton<IRabbitMessageQueueSettings, RabbitMessageQueueSettings>();
            services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();

            services.AddSingleton<IMessagePublisher>(implementationFactory: sp => new RabbitMessageQueueMessagePublisher(
                settings: sp.GetService<IRabbitMessageQueueSettings>(),
                exchange: "SearchLog",
                messageSerializer: sp.GetService<IMessageSerializer>(),
                logger: sp.GetService<ILogger<RabbitMessageQueueMessagePublisher>>()));

            services
                .AddControllers()
                .AddNewtonsoftJson();

            services.AddSignalR(signalrConfiguration =>
            {
                signalrConfiguration.EnableDetailedErrors = true;
            });

            services.AddSingleton<IElasticClientSettings, ElasticClientSettings>();
            services.AddSingleton<IProductsRepository, ProductsRepository>();
            services.AddSingleton(typeof(ICache<>), typeof(RedisCache<>));
            services.AddSingleton<IRedisCacheSettings, RedisCacheSettings>();

            services.AddSingleton<IMessageHandler<ProductCreatedEventHandler>>(serviceprovider => new RabbitMessageQueueMessageHandler<ProductCreatedEventHandler>(
                settings: serviceprovider.GetService<IRabbitMessageQueueSettings>(),
                exchange: "Product",
                queue: "Product:Event:ProductCreatedEvent",
                routingKey: "Event:ProductCreatedEvent",
                messageSerializer: serviceprovider.GetService<IMessageSerializer>(),
                logger: serviceprovider.GetService<ILogger<RabbitMessageQueueMessageHandler<ProductCreatedEventHandler>>>()));

            services.AddSingleton<IMessageHandler<ProductNameChangedEventHandler>>(serviceprovider => new RabbitMessageQueueMessageHandler<ProductNameChangedEventHandler>(
                settings: serviceprovider.GetService<IRabbitMessageQueueSettings>(),
                exchange: "Product",
                queue: "Product:Event:ProductNameChangedEvent",
                routingKey: "Event:ProductNameChangedEvent",
                messageSerializer: serviceprovider.GetService<IMessageSerializer>(),
                logger: serviceprovider.GetService<ILogger<RabbitMessageQueueMessageHandler<ProductNameChangedEventHandler>>>()));
            
            services
                .AddHostedService<ProductNameChangedEventHandler>()
                .AddHostedService<ProductCreatedEventHandler>();

            services.AddHealthChecks(checks: checks =>
            {
                checks.AddCheck(name: "AlwaysAvailable", check: () => new AlwaysAvailableCheck());
                checks.WithDefaultCacheDuration(duration: 3.Seconds());
                checks.WithPartialSuccessStatus(partiallyHealthyStatus: CheckStatus.Healthy);
            });
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            SetupAutoMapper();

            //app.UseHttpsRedirection();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseAuthorization();

            app.UseSignalR(routes =>
            {
                routes.MapHub<ProductHub>("/producthub");
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
