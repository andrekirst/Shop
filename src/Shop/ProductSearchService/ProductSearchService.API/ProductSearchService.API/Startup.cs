﻿using System;
using AutoMapper;
using FluentTimeSpan;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProductSearchService.API.Caching;
using ProductSearchService.API.Checks;
using ProductSearchService.API.Commands;
using ProductSearchService.API.EventHandlers;
using ProductSearchService.API.Events;
using ProductSearchService.API.Hubs;
using ProductSearchService.API.Messaging;
using ProductSearchService.API.Model;
using ProductSearchService.API.Repositories;

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

            services.AddMemoryCache();

            services.AddApiVersioning(setupAction: versioningSetup =>
            {
                versioningSetup.AssumeDefaultVersionWhenUnspecified = true;
                versioningSetup.DefaultApiVersion = new ApiVersion(majorVersion: 1, minorVersion: 0);
                versioningSetup.RegisterMiddleware = true;
                versioningSetup.ReportApiVersions = true;
                versioningSetup.UseApiBehavior = true;
            });

            services.AddSignalR(configure: signalrConfiguration =>
            {
                signalrConfiguration.EnableDetailedErrors = true;
            });

            services.AddSingleton<IElasticClientSettings, ElasticClientSettings>();
            services.AddSingleton<IProductsRepository, ProductsRepository>();
            services.AddSingleton<ICache, RedisCache>();
            services.AddSingleton<IRedisCacheSettings, RedisCacheSettings>();

            services.AddSingleton<IMessageHandler<ProductCreatedEventHandler>>(implementationFactory: serviceprovider => new RabbitMessageQueueMessageHandler<ProductCreatedEventHandler>(
                settings: serviceprovider.GetService<IRabbitMessageQueueSettings>(),
                exchange: "Product",
                queue: "Product:Event:ProductCreatedEvent",
                routingKey: "Event:ProductCreatedEvent",
                messageSerializer: serviceprovider.GetService<IMessageSerializer>(),
                logger: serviceprovider.GetService<ILogger<RabbitMessageQueueMessageHandler<ProductCreatedEventHandler>>>()));

            services.AddSingleton<IMessageHandler<ProductNameChangedEventHandler>>(implementationFactory: serviceprovider => new RabbitMessageQueueMessageHandler<ProductNameChangedEventHandler>(
                settings: serviceprovider.GetService<IRabbitMessageQueueSettings>(),
                exchange: "Product",
                queue: "Product:Event:ProductNameChangedEvent",
                routingKey: "Event:ProductNameChangedEvent",
                messageSerializer: serviceprovider.GetService<IMessageSerializer>(),
                logger: serviceprovider.GetService<ILogger<RabbitMessageQueueMessageHandler<ProductNameChangedEventHandler>>>()));

            services.AddSingleton<IMessageHandler<ProductsCreatedEventHandler>>(implementationFactory: serviceprovider => new RabbitMessageQueueMessageHandler<ProductsCreatedEventHandler>(
                settings: serviceprovider.GetService<IRabbitMessageQueueSettings>(),
                exchange: "Product",
                queue: "Product:Event:ProductsCreatedEvent",
                routingKey: "Event:ProductsCreatedEvent",
                messageSerializer: serviceprovider.GetService<IMessageSerializer>(),
                logger: serviceprovider.GetService<ILogger<RabbitMessageQueueMessageHandler<ProductsCreatedEventHandler>>>()));

            services
                .AddHostedService<ProductNameChangedEventHandler>()
                .AddHostedService<ProductCreatedEventHandler>()
                .AddHostedService<ProductsCreatedEventHandler>();

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

            app.UseEndpoints(configure: endpoints =>
            {
                endpoints.MapControllers();
            });

            app.UseAuthorization();

            app.UseSignalR(configure: routes =>
            {
                routes.MapHub<ProductHub>(path: "/producthub");
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
