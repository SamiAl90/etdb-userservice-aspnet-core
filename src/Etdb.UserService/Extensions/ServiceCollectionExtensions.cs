﻿using System.IO;
using System.IO.Compression;
using System.Threading;
using Etdb.ServiceBase.Filter;
using Etdb.UserService.AspNetCore.Filter;
using Etdb.UserService.AspNetCore.Policies;
using Etdb.UserService.Authentication.Abstractions.Services;
using Etdb.UserService.Authentication.Configuration;
using Etdb.UserService.Authentication.Services;
using Etdb.UserService.Authentication.Validator;
using Etdb.UserService.Autofac.Extensions;
using Etdb.UserService.Misc.Configuration;
using Etdb.UserService.Misc.Constants;
using Etdb.UserService.Repositories;
using Etdb.UserService.Services;
using Etdb.UserService.Services.Abstractions;
using IdentityServer4.Contrib.Caching.Redis.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Etdb.UserService.Extensions
{
    public static class ServiceCollectionExtensions
    {
        private const string FilesLocalSubPath = "Files";

        private static readonly string CookieName =
            typeof(Startup)!.Assembly!.GetName()!.Name!.Replace(".dll", "").Replace(".exe", "");


        public static IServiceCollection ConfigureResponseCompression(this IServiceCollection services,
            CompressionLevel level = CompressionLevel.Fastest)
        {
            return services
                .Configure<GzipCompressionProviderOptions>(options => options.Level = level)
                .Configure<BrotliCompressionProviderOptions>(options => options.Level = level)
                .AddResponseCompression(options =>
                {
                    options.Providers.Add<BrotliCompressionProvider>();
                    options.Providers.Add<GzipCompressionProvider>();
                    options.MimeTypes = new[]
                    {
                        "application/json",
                        "application/json; charset=utf-8",
                        "image/*"
                    };
                });
        }

        public static IServiceCollection ConfigureFileStoreOptions(this IServiceCollection services,
            IConfiguration configuration, IWebHostEnvironment environment)
        {
            return services.Configure<FilestoreConfiguration>(options =>
            {
                if (environment.IsAnyDevelopment() || environment.IsAzureDevelopment())
                {
                    options.ImagePath = Path.Combine(environment.ContentRootPath,
                        ServiceCollectionExtensions.FilesLocalSubPath);

                    return;
                }

                configuration.GetSection(nameof(FilestoreConfiguration)).Bind(options);
            });
        }

        public static IServiceCollection ConfigureIdentityServerConfigurationOptions(this IServiceCollection services,
            IConfiguration configuration)
        {
            return services.Configure<IdentityServerConfiguration>(options =>
            {
                configuration.GetSection(nameof(IdentityServerConfiguration)).Bind(options);
            });
        }

        public static IServiceCollection ConfigureDocumentDbContextOptions(this IServiceCollection services,
            IConfiguration configuration)
        {
            return services.Configure<DocumentDbContextOptions>(options => configuration
                .GetSection(nameof(DocumentDbContextOptions))
                .Bind(options));
        }

        public static IServiceCollection ConfigureSwaggerGen(this IServiceCollection services, OpenApiInfo openApiInfo,
            string title)
        {
            services.AddMvcCore()
                .AddApiExplorer();

            return services.AddSwaggerGen(options => options.SwaggerDoc(title, openApiInfo));
        }

        public static IServiceCollection ConfigureAllowedOriginsOptions(this IServiceCollection services,
            IConfiguration configuration)
        {
            return services.Configure<AllowedOriginsConfiguration>(options => configuration
                .GetSection(nameof(AllowedOriginsConfiguration))
                .Bind(options));
        }

        public static IServiceCollection ConfigureApiControllers(this IServiceCollection services)
        {
            services.AddControllers(options =>
                {
                    options.EnableEndpointRouting = true;
                    options.OutputFormatters.RemoveType<XmlSerializerOutputFormatter>();
                    options.InputFormatters.RemoveType<XmlSerializerInputFormatter>();

                    var requireAuthenticatedUserPolicy = new AuthorizeFilter(
                        new AuthorizationPolicyBuilder()
                            .RequireAuthenticatedUser()
                            .Build());

                    options.Filters.Add(requireAuthenticatedUserPolicy);

                    options.Filters.Add<UnhandledExceptionFilter>();
                    options.Filters.Add<IdentityServerExceptionFilter>();
                    options.Filters.Add<AccessDeniedExceptionFilter>();
                    options.Filters.Add<GeneralValidationExceptionFilter>();
                    options.Filters.Add<ResourceLockedExceptionFilter>();
                    options.Filters.Add<ResourceNotFoundExceptionFilter>();
                })
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_3_0);

            return services.Configure<ApiBehaviorOptions>(options =>
            {
                options.SuppressModelStateInvalidFilter = true;
            });
        }

        public static IServiceCollection ConfigureHttpClients(this IServiceCollection services)
        {
            services.AddHttpClient<IIdentityServerClient, IdentityServerClient>()
                .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

            services.AddHttpClient<IExternalIdentityServerClient, ExternalIdentityServerClient>()
                .SetHandlerLifetime(Timeout.InfiniteTimeSpan);

            return services;
        }

        public static IServiceCollection ConfigureCors(this IServiceCollection services,
            IWebHostEnvironment environment, string[] allowedOrigins, string policyName)
        {
            return services.AddCors(options =>
            {
                options.AddPolicy(policyName, builder =>
                {
                    builder.AllowAnyHeader()
                        .AllowAnyMethod();

                    if (environment.IsAnyDevelopment())
                    {
                        builder.AllowAnyOrigin();
                        return;
                    }

                    builder.WithOrigins(allowedOrigins);
                });
            });
        }

        public static IServiceCollection ConfigureIdentityServerAuthorization(this IServiceCollection services,
            IdentityServerConfiguration identityServerConfiguration, RedisCacheOptions redisCacheOptions,
            IWebHostEnvironment environment)
        {
            var identityServerBuilder = services.AddIdentityServer(options =>
                {
                    options.Authentication.CookieAuthenticationScheme = ServiceCollectionExtensions.CookieName;
                })
                .AddDeveloperSigningCredential()
                .AddInMemoryIdentityResources(IdentityResourceConfiguration.GetIdentityResource())
                .AddInMemoryApiResources(ApiResourceConfiguration.GetApiResource())
                .AddInMemoryClients(ClientConfiguration.GetClients(identityServerConfiguration))
                .AddProfileService<ProfileService>()
                .AddResourceOwnerValidator<ResourceOwnerPasswordGrantValidator>()
                .AddExtensionGrantValidator<ExternalGrantValidator>();

            if (environment.IsAzureDevelopment())
            {
                identityServerBuilder.AddInMemoryPersistedGrants();
                return services;
            }

            identityServerBuilder.AddDistributedRedisCache(redisCacheOptions.Configuration,
                redisCacheOptions.InstanceName);

            return services;
        }

        public static IServiceCollection ConfigureIdentityServerAuthentication(this IServiceCollection services,
            IWebHostEnvironment environment, string schema, string apiName, string authority)
        {
            services.AddAuthentication(schema)
                .AddCookie(ServiceCollectionExtensions.CookieName)
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = authority;
                    options.RequireHttpsMetadata = false;
                    options.IntrospectionDiscoveryPolicy.RequireHttps = false;
                    options.ApiName = apiName;
                });

            return services;
        }

        public static IServiceCollection ConfigureAuthorizationPolicies(this IServiceCollection services)
        {
            return services.AddAuthorization(options =>
                {
                    options.AddPolicy(RolePolicyNames.AdminPolicy, builder => builder.RequireRole(RoleNames.Admin));
                })
                .AddScoped<IAuthorizationHandler, AuthenticatedUserIsRequestedUserHandler>();
        }
    }
}