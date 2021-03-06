﻿using System;
using System.Linq;
using System.Net;
using AutoMapper;
using FluentTimeSpan;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProductSearchService.API.Checks;
using ProductSearchService.API.Commands;
using ProductSearchService.API.EventHandlers;
using ProductSearchService.API.Events;
using ProductSearchService.API.Hubs;
using ProductSearchService.API.Logging;
using ProductSearchService.API.Model;
using ProductSearchService.API.Repositories;
using Shop.Infrastructure.Caching;
using Shop.Infrastructure.Infrastructure;
using Shop.Infrastructure.Infrastructure.Json;
using Shop.Infrastructure.Messaging;

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
            services.AddTransient<ICorrelationIdFactory, DefaultCorrelationIdFactory>();
            services.AddSingleton<IRabbitMessageQueueSettings, RabbitMessageQueueSettings>();
            services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
            services.AddMemoryCache();

            services.AddSingleton<IMessagePublisher>(implementationFactory: sp => new RabbitMessageQueueMessagePublisher(
                settings: sp.GetService<IRabbitMessageQueueSettings>(),
                messageSerializer: sp.GetService<IMessageSerializer>(),
                logger: sp.GetService<ILogger<RabbitMessageQueueMessagePublisher>>(),
                memoryCache: sp.GetService<IMemoryCache>()));

            services
                .AddControllers()
                .AddNewtonsoftJson();

            services.AddTransient<IDateTimeProvider, DefaultDateTimeProvider>();
            services.AddTransient<IJsonSerializer, NewtonsoftJsonSerializer>();
            services.AddSingleton<IShopApiLogging, ShopApiLogging>(implementationFactory: sp => new ShopApiLogging(
                messagePublisher: sp.GetService<IMessagePublisher>(),
                dateTimeProvider: sp.GetService<IDateTimeProvider>(),
                loggingOptions: new ShopLoggingOptions
                {
                    Environment = Environment.GetEnvironmentVariable(variable: "ASPNETCORE_ENVIRONMENT"),
                    ServiceArea = "Private",
                    ServiceName = "ProductSearchService.API",
                    ServiceVersion = "1.0",
                    HostIPAddresses = Dns.GetHostAddresses(hostNameOrAddress: Dns.GetHostName())
                        .Select(selector: host =>
                            host.MapToIPv4().ToString())
                        .ToList(),
                    HostName = Dns.GetHostName()
                }));

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
                AppContext.SetSwitch(switchName: "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", isEnabled: true);
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
                endpoints.MapHub<ProductHub>(pattern: "/producthub");
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
