using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.HealthChecks;
using ProductSearchService.API.DataAccess;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Serilog;
using ProductSearchService.API.Repositories;

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

            services.AddDbContext<ProductSearchDbContext>(optionsAction: options =>
            {
                options.UseSqlServer(connectionString: connectionString);
                options.EnableDetailedErrors();
                options.UseQueryTrackingBehavior(queryTrackingBehavior: QueryTrackingBehavior.NoTracking);
            });

            services
                .AddMvc()
                .AddNewtonsoftJson();

            services.AddTransient<IProductsRepository, ProductsRepository>();

            services.AddSwaggerGen(setupAction: c =>
            {
                c.SwaggerDoc(name: "v1", info: new Info { Title = "ProductSearchService.API", Version = "v1" });
            });

            services.AddHealthChecks(checks: checks =>
            {
                checks.WithDefaultCacheDuration(duration: TimeSpan.FromSeconds(value: 1));
                checks.AddSqlCheck(name: "ProductSearchConnectionString", connectionString: ConnectionString);
            });
        }

        private string ConnectionString => Configuration.GetConnectionString(name: "ProductSearchConnectionString");

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime, ProductSearchDbContext dbContext)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
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

            using (var scope = app.ApplicationServices.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                scope.ServiceProvider.GetService<ProductSearchDbContext>().MigrateDB();
            }

            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //}
            //else
            //{
            //    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            //    app.UseHsts();
            //}

            //app.UseHttpsRedirection();

            //app.UseRouting(routes =>
            //{
            //    routes.MapApplication();
            //});

            //app.UseAuthorization();
        }

        private void SetupAutoMapper()
        {
            Mapper.Initialize(config: config =>
            {
            });
        }
    }
}
