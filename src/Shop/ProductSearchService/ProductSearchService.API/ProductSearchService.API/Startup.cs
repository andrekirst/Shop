﻿using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AutoMapper;
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
            services.AddSingleton<IRabbitMessageQueueSettings, RabbitMessageQueueSettings>();
            services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();

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

            services.AddSingleton<IMessagePublisher>(implementationFactory: sp => new RabbitMessageQueueMessagePublisher(
                settings: sp.GetService<IRabbitMessageQueueSettings>(),
                exchange: "SearchLog",
                messageSerializer: sp.GetService<IMessageSerializer>(),
                logger: sp.GetService<ILogger<RabbitMessageQueueMessagePublisher>>()));

            services
                .AddMvc()
                .AddNewtonsoftJson();

            services.AddSingleton<IElasticClientSettings, ElasticClientSettings>();
            services.AddSingleton<IProductsRepository, ProductsRepository>();
            services.AddSingleton(typeof(ICache<>), typeof(RedisCache<>));
            services.AddSingleton<IRedisCacheSettings, RedisCacheSettings>();

            services.AddHealthChecks(checks: checks =>
            {
                checks.AddCheck(name: "AlwaysAvailable", check: () => new AlwaysAvailableCheck());
                checks.WithDefaultCacheDuration(duration: 3.Seconds());
                checks.WithPartialSuccessStatus(partiallyHealthyStatus: CheckStatus.Healthy);
            });
        }

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

            //app.UseHttpsRedirection();

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
