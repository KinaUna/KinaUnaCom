﻿using IdentityServer4.AccessTokenValidation;
using KinaUna.Data.Contexts;
using KinaUnaMediaApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


namespace KinaUnaMediaApi
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
            var authorityServerUrl = Configuration.GetValue<string>("AuthenticationServer");
            var authenticationServerClientId = Configuration.GetValue<string>("AuthenticationServerClientId");
            var authenticationServerClientSecret = Configuration["AuthenticationServerClientSecret"];

            services.AddDbContext<MediaDbContext>(options =>
                options.UseSqlServer(Configuration["MediaDefaultConnection"]));

            services.AddDbContext<ProgenyDbContext>(options =>
                options.UseSqlServer(Configuration["ProgenyDefaultConnection"]));

            services.AddDistributedRedisCache(option =>
                option.Configuration = Configuration["RedisConnection"]);

            services.AddScoped<IDataService, DataService>();

            services.AddControllers().AddNewtonsoftJson();
            services.AddSingleton<ImageStore>();
            services.AddScoped<AzureNotifications>();
            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = authorityServerUrl;
                    options.ApiName = authenticationServerClientId;
                    options.ApiSecret = authenticationServerClientSecret;
                    options.RequireHttpsMetadata = true;
                });
            services.AddAuthorization();
            services.AddApplicationInsightsTelemetry();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
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

            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}
