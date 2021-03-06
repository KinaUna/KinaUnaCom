﻿using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Services;
using KinaUna.IDP.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using KinaUna.Data;
using KinaUna.Data.Contexts;
using KinaUna.Data.Models;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;

namespace KinaUna.IDP
{
    public class Startup
    {
        private IConfiguration Configuration { get; }
        private readonly IWebHostEnvironment _env;

        public Startup(IConfiguration configuration, IWebHostEnvironment environment)
        {
            Configuration = configuration;
            _env = environment;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;

            services.AddDbContext<ProgenyDbContext>(options =>
                options.UseSqlServer(Configuration["ProgenyDefaultConnection"],
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }));

            services.AddDbContext<WebDbContext>(options =>
                options.UseSqlServer(Configuration["WebDefaultConnection"],
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }));


            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(Configuration["DataProtectionConnection"],
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }));

            services.AddDbContext<MediaDbContext>(options =>
                options.UseSqlServer(Configuration["MediaDefaultConnection"],
                    sqlServerOptionsAction: sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(Startup).GetTypeInfo().Assembly.GetName().Name);
                        //Configuring Connection Resiliency: https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-resiliency 
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 15, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
                    }));

            var credentials = new StorageCredentials(Constants.CloudBlobUsername, Configuration["BlobStorageKey"]);
            CloudBlobClient blobClient = new CloudBlobClient(new Uri(Constants.CloudBlobBase), credentials);
            CloudBlobContainer container = blobClient.GetContainerReference("dataprotection");

            container.CreateIfNotExistsAsync().GetAwaiter().GetResult();

            services.AddDataProtection()
                .SetApplicationName("KinaUnaWebApp")
                .PersistKeysToAzureBlobStorage(container, "kukeys.xml");

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
                            builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                        });
                });
            }
            else
            {
                services.AddCors(options =>
                {
                    options.AddDefaultPolicy(builder =>
                    {
                        builder.WithOrigins(Constants.WebAppUrl, "https://" + Constants.AppRootDomain);
                    });
                    options.AddPolicy("KinaUnaCors",
                        builder =>
                        {
                            builder.WithOrigins("https://*." + Constants.AppRootDomain).SetIsOriginAllowedToAllowWildcardSubdomains().AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                        });
                });
            }


            services.AddControllersWithViews()
                .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix);

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
                })
                .AddRedisCaching(options =>
                {
                    options.RedisConnectionString = Configuration["RedisConnection"];
                    options.KeyPrefix = Constants.AppName + "idp";
                })
                .AddClientStoreCache<IdentityServer4.EntityFramework.Stores.ClientStore>()
                .AddResourceStoreCache<IdentityServer4.EntityFramework.Stores.ResourceStore>()
                .AddCorsPolicyCache<IdentityServer4.EntityFramework.Services.CorsPolicyService>()
                .AddProfileServiceCache<ProfileService>()
                .Services.AddTransient<IProfileService, ProfileService>();

            services.AddAuthentication().AddGoogle("Google", "Google", options =>
            {
                options.ClientId = Configuration["GoogleClientId"];
                options.ClientSecret = Configuration["GoogleClientSecret"];
            }).AddFacebook("Facebook", "Facebook", options =>
            {
                options.ClientId = Configuration["FacebookClientId"];
                options.ClientSecret = Configuration["FacebookClientSecret"];
            }).AddMicrosoftAccount("Microsoft", "Microsoft", microsoftOptions =>
            {
                microsoftOptions.ClientId = Configuration["MicrosoftClientId"];
                microsoftOptions.ClientSecret = Configuration["MicrosoftClientSecret"];
            });

            services.AddApplicationInsightsTelemetry();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment hostingEnvironment
            )
        {
            // This will do the initial DB population
            InitializeDatabase(app, Constants.ResetIdentityDb);

            if (_env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

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
            app.UseStaticFiles();
            app.UseRouting();
            app.UseCors("KinaUnaCors");
            app.UseIdentityServer();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints => {
                endpoints.MapDefaultControllerRoute();
            });
        }

        private void InitializeDatabase(IApplicationBuilder app, bool resetDb)
        {
            using (var serviceScope = app.ApplicationServices.GetService<IServiceScopeFactory>().CreateScope())
            {
                serviceScope.ServiceProvider.GetRequiredService<PersistedGrantDbContext>().Database.Migrate();

                var context = serviceScope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
                context.Database.Migrate();

                var usersContext = serviceScope.ServiceProvider.GetRequiredService<ProgenyDbContext>();

                usersContext.Database.Migrate();

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

                if (usersContext.UserInfoDb.SingleOrDefault(u => u.UserEmail.ToUpper() == Constants.DefaultUserEmail.ToUpper()) == null)
                {
                    UserInfo userInfo = new UserInfo();
                    userInfo.UserEmail = Constants.DefaultUserEmail;
                    userInfo.FirstName = "System";
                    userInfo.LastName = "Default User";
                    userInfo.Timezone = Constants.DefaultTimezone;
                    userInfo.UserName = Constants.DefaultUserEmail;
                    userInfo.ViewChild = Constants.DefaultChildId;

                    usersContext.UserInfoDb.Add(userInfo);
                    usersContext.SaveChanges();
                }

                if (usersContext.ProgenyDb.SingleOrDefault(p => p.Id == Constants.DefaultChildId) == null)
                {
                    Progeny progeny = new Progeny();
                    progeny.Admins = Constants.AdminEmail;
                    progeny.BirthDay = DateTime.UtcNow;
                    progeny.Name = Constants.AppName;
                    progeny.NickName = Constants.AppName;
                    progeny.PictureLink = Constants.ProfilePictureUrl;
                    progeny.TimeZone = Constants.DefaultTimezone;

                    usersContext.ProgenyDb.Add(progeny);
                    usersContext.SaveChanges();
                }

                if (usersContext.UserAccessDb.SingleOrDefault(u =>
                        u.ProgenyId == Constants.DefaultChildId &&
                        u.UserId.ToUpper() == Constants.DefaultUserEmail.ToUpper()) == null)
                {
                    UserAccess userAccess = new UserAccess();
                    userAccess.ProgenyId = Constants.DefaultChildId;
                    userAccess.UserId = Constants.DefaultUserEmail.ToUpper();
                    userAccess.AccessLevel = (int)AccessLevel.Users;
                    userAccess.CanContribute = false;

                    usersContext.UserAccessDb.Add(userAccess);
                    usersContext.SaveChanges();
                }

                if (usersContext.UserAccessDb.SingleOrDefault(u =>
                        u.ProgenyId == Constants.DefaultChildId &&
                        u.UserId.ToUpper() == Constants.AdminEmail.ToUpper()) == null)
                {
                    UserAccess userAccess = new UserAccess();
                    userAccess.ProgenyId = Constants.DefaultChildId;
                    userAccess.UserId = Constants.AdminEmail.ToUpper();
                    userAccess.AccessLevel = (int)AccessLevel.Private;
                    userAccess.CanContribute = true;

                    usersContext.UserAccessDb.Add(userAccess);
                    usersContext.SaveChanges();
                }
            }
        }
    }
}
