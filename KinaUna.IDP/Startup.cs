﻿using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Services;
using KinaUna.IDP.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;

namespace KinaUna.IDP
{
    public class Startup
    {
        private IConfiguration Configuration { get; }
        private readonly IHostingEnvironment _env;
        private readonly ILoggerFactory _loggerFactory;

        public Startup(IConfiguration configuration, IHostingEnvironment environment, ILoggerFactory loggerFactory)
        {
            Configuration = configuration;
            _env = environment;
            _loggerFactory = loggerFactory;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration["AuthDefaultConnection"],
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }));
            services.AddDbContext<ProgenyDbContext>(options =>
                options.UseSqlServer(Configuration["ProgenyDefaultConnection"]));

            services.AddSingleton<IXmlRepository, DataProtectionKeyRepository>();

            var built = services.BuildServiceProvider();
            services.AddDataProtection().AddKeyManagementOptions(options => options.XmlRepository = built.GetService<IXmlRepository>()).SetApplicationName("KinaUnaWebApp");

            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
                {
                    options.Password.RequireNonAlphanumeric = false;
                    options.Password.RequireUppercase = false;
                    options.Password.RequireLowercase = false;
                    options.Password.RequireDigit = false;
                    options.SignIn.RequireConfirmedEmail = true;
                    options.User.RequireUniqueEmail = true;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.Configure<AppSettings>(Configuration);

            services.Configure<DataProtectionTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromDays(90);
            });

            services.AddTransient<ILoginService<ApplicationUser>, EfLoginService>();
            services.AddTransient<IRedirectService, RedirectService>();
            services.AddTransient<IEmailSender, EmailSender>();

            services.AddLocalization(o =>
            {
                o.ResourcesPath = "Resources";
            });
            
            X509Certificate2 cert = null;
            using (X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser))
            {
                certStore.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certCollection = certStore.Certificates.Find(
                    X509FindType.FindByThumbprint,
                    Configuration["X509ThumbPrint"],
                    false);
                // Get the first cert with the thumbprint
                if (certCollection.Count > 0)
                {
                    cert = certCollection[0];
                }
            }

            if (_env.IsDevelopment())
            {
                services.AddCors(options =>
                {
                    options.AddPolicy("KinaUnaCors",
                        builder =>
                        {
                            builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                        });
                });
            }
            else
            {
                services.AddCors(options =>
                {
                    options.AddPolicy("KinaUnaCors",
                        builder =>
                        {
                            builder.WithOrigins("https://*." + Constants.AppRootDomain).SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                        });
                });
                var cors = new DefaultCorsPolicyService(_loggerFactory.CreateLogger<DefaultCorsPolicyService>())
                {
                    AllowedOrigins = { Constants.WebAppUrl, "https://" + Constants.AppRootDomain }
                };
                services.AddSingleton<ICorsPolicyService>(cors);
            }
            

            services.AddMvc().AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix);
            

            services.AddIdentityServer(x =>
                {
                    x.Authentication.CookieLifetime = TimeSpan.FromDays(90);
                    x.Authentication.CookieSlidingExpiration = true;
                })
                .AddSigningCredential(cert)
                .AddAspNetIdentity<ApplicationUser>()
                // This adds the config data from DB (clients, resources)
                .AddConfigurationStore(options =>
                {
                    options.ConfigureDbContext = builder =>
                        builder.UseSqlServer(Configuration["AuthDefaultConnection"],
                            sql => sql.MigrationsAssembly(migrationsAssembly));
                })
                // This adds the operational data from DB (codes, tokens, consents)
                .AddOperationalStore(options =>
                {
                    options.ConfigureDbContext = builder =>
                        builder.UseSqlServer(Configuration["AuthDefaultConnection"],
                            sql => sql.MigrationsAssembly(migrationsAssembly));

                    // This enables automatic token cleanup. this is optional.
                    options.EnableTokenCleanup = true;
                    options.TokenCleanupInterval = 3600;
                }).Services.AddTransient<IProfileService, ProfileService>();

        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment hostingEnvironment
            )
        {
            // This will do the initial DB population
            bool resetDb = false;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            InitializeDatabase(app, resetDb);

            if (_env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            app.UseCors("KinaUnaCors");

            var supportedCultures = new[]
            {
                new CultureInfo("en-US"),
                new CultureInfo("da-DK"),
                new CultureInfo("de-DE"),
            };

            RequestLocalizationOptions localizationOptions = new RequestLocalizationOptions
            {
                DefaultRequestCulture = new RequestCulture("en-US"),
                SupportedCultures = supportedCultures,
                SupportedUICultures = supportedCultures
            };
            var provider = new CookieRequestCultureProvider()
            {
                CookieName = Constants.LanguageCookieName
            };
            localizationOptions.RequestCultureProviders.Insert(0, provider);

            app.UseRequestLocalization(localizationOptions);

            app.UseIdentityServer();
            app.UseStaticFiles();
            app.UseMvcWithDefaultRoute();
        }

        private void InitializeDatabase(IApplicationBuilder app, bool resetDb)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                context.Database.Migrate();
                
                if (resetDb)
                {
                    var contextClients = context.Clients.ToList();
                    foreach (var clientToDelete in contextClients)
                    {
                        context.Clients.Remove(clientToDelete);
                    }

                    var contextApis = context.ApiResources.ToList();
                    foreach (var apiToDelete in contextApis)
                    {
                        context.ApiResources.Remove(apiToDelete);
                    }

                    var contextIdentities = context.IdentityResources.ToList();
                    foreach (var identityToDelete in contextIdentities)
                    {
                        context.IdentityResources.Remove(identityToDelete);
                    }

                    context.SaveChanges();
                }

                if (!context.Clients.Any())
                {
                    foreach (var client in Config.GetClients(Configuration))
                    {
                        context.Clients.Add(client.ToEntity());
                    }
                    context.SaveChanges();
                }
                
                if (!context.IdentityResources.Any())
                {
                    foreach (var resource in Config.GetIdentityResources())
                    {
                        context.IdentityResources.Add(resource.ToEntity());
                        
                    }
                    context.SaveChanges();
                }
                

                if (!context.ApiResources.Any())
                {
                    foreach (var resource in Config.GetApiResources(Configuration))
                    {
                        context.ApiResources.Add(resource.ToEntity());
                    }
                    context.SaveChanges();
                }
            }
        }
    }
}
