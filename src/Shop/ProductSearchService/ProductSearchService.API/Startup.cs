using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductSearchService.API.DataAccess;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

namespace ProductSearchService.API
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ProductSearchDbContext>(options => options.UseSqlServer(ConnectionString));

            services.AddMvc()
                .AddNewtonsoftJson();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Info { Title = "ProductSearchService.API", Version = "v1" });
            });

            services.AddHealthChecks(checks =>
            {
                checks.WithDefaultCacheDuration(TimeSpan.FromSeconds(1));
                checks.AddSqlCheck("ProductSearchConnectionString", ConnectionString);
            });
        }

        private string ConnectionString => Configuration.GetConnectionString("ProductSearchConnectionString");

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime, ProductSearchDbContext dbContext)
        {
            app.UseMvc();
            app.UseDefaultFiles();
            app.UseStaticFiles();

            SetupAutoMapper();

            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductSearchServiceAPI - v1");
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
            Mapper.Initialize(config =>
            {
            });
        }
    }
}
