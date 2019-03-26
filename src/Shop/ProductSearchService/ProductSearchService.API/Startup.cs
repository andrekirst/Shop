using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.AspNetCore.Swagger;
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
            services.AddTransient<IMessagePublisher>(implementationFactory: sp => new RabbitMessageQueueMessagePublisher(
                hostname: configSection[key: "Hostname"],
                username: configSection[key: "Username"],
                password: configSection[key: "Password"],
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

            services.AddHealthChecks(checks: checks =>
            {
                checks.AddCheck(name: "AlwaysAvailable", check: () => new AlwaysAvailableCheck());
                checks.WithDefaultCacheDuration(3.Seconds());
                checks.WithPartialSuccessStatus(CheckStatus.Healthy);
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
