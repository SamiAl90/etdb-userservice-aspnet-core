﻿using System.IO;
using System.IO.Compression;
using Autofac;
using Autofac.Extensions.FluentBuilder;
using AutoMapper.Extensions.Autofac.DependencyInjection;
using Etdb.ServiceBase.Constants;
using Etdb.ServiceBase.Cqrs.Abstractions.Bus;
using Etdb.ServiceBase.Cqrs.Abstractions.Validation;
using Etdb.ServiceBase.Cqrs.Bus;
using Etdb.ServiceBase.Cryptography.Abstractions.Hashing;
using Etdb.ServiceBase.Cryptography.Hashing;
using Etdb.ServiceBase.DocumentRepository.Abstractions.Context;
using Etdb.ServiceBase.DocumentRepository.Abstractions.Generics;
using Etdb.ServiceBase.Filter;
using Etdb.ServiceBase.Services;
using Etdb.ServiceBase.Services.Abstractions;
using Etdb.UserService.AutoMapper.Profiles;
using Etdb.UserService.Bootstrap.Config;
using Etdb.UserService.Bootstrap.Services;
using Etdb.UserService.Constants;
using Etdb.UserService.Controllers.V1;
using Etdb.UserService.Cqrs.Handler;
using Etdb.UserService.Extensions;
using Etdb.UserService.Repositories;
using Etdb.UserService.Services;
using Etdb.UserService.Services.Abstractions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Validation;
using MediatR.Extensions.Autofac.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Cors.Internal;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;

namespace Etdb.UserService.Bootstrap
{
    public class Startup
    {
        private readonly IConfigurationRoot configuration;
        private readonly IHostingEnvironment environment;

        private const string SwaggerDocDescription = "Etdb " + ServiceNames.UserService + " V1";
        private const string SwaggerDocVersion = "v1";
        private const string SwaggerDocJsonUri = "/swagger/v1/swagger.json";
        private const string CorsPolicyName = "AllowAll";
        private const string AuthenticationSchema = "Bearer";
        private const string DbContextOptions = "DocumentDbContextOptions";
        private const string RedisCacheOptions = "RedisCacheOptions";

        public Startup(IHostingEnvironment environment)
        {
            this.environment = environment;

            this.configuration = new ConfigurationBuilder()
                .SetBasePath(environment.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{environment.EnvironmentName}.json", true, true)
                .AddEnvironmentVariables()
                .Build();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            var identityConfig = this.configuration
                .GetSection("IdentityConfig");

            var allowedOrigins = identityConfig
                .GetSection("Origins")
                .Get<string[]>();

            var clientId = identityConfig
                .GetSection("WebClient")
                .GetValue<string>("Name");

            var clientSecret = identityConfig
                .GetSection("WebClient")
                .GetValue<string>("Secret");

            var authority = identityConfig
                .GetValue<string>("Authority");

            services.AddMvc(options =>
                {
                    options.OutputFormatters.RemoveType<XmlSerializerOutputFormatter>();
                    options.InputFormatters.RemoveType<XmlSerializerInputFormatter>();

                    var requireAuthenticatedUserPolicy = new AuthorizeFilter(new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .Build());

                    options.Filters.Add(requireAuthenticatedUserPolicy);
                    options.Filters.Add(typeof(UnhandledExceptionFilter));
                    options.Filters.Add(typeof(ConcurrencyExceptionFilter));
                    options.Filters.Add(typeof(AccessDeniedExceptionFilter));
                    options.Filters.Add(typeof(GeneralValidationExceptionFilter));
                    options.Filters.Add(typeof(ResourceNotFoundExceptionFilter));
                })
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                    options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
                    options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                })
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            services.AddIdentityServer()
                .AddDeveloperSigningCredential()
                .AddInMemoryIdentityResources(IdentityResourceConfig.GetIdentityResource())
                .AddInMemoryApiResources(ApiResourceConfig.GetApiResource())
                .AddInMemoryClients(ClientConfig.GetClients(clientId, clientSecret, allowedOrigins));

            services.AddAuthentication(Startup.AuthenticationSchema)
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = authority;
                    options.RequireHttpsMetadata = this.environment.IsProduction();
                    options.ApiName = ServiceNames.UserService;
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy(RolePolicyNames.AdminPolicy, builder => builder.RequireRole(RoleNames.Admin));
            });

            services.AddCors(options =>
                {
                    options.AddPolicy(Startup.CorsPolicyName, builder =>
                    {
                        builder.AllowAnyHeader()
                            .AllowAnyMethod();

                        if (this.environment.IsDevelopment() || this.environment.IsLocalDevelopment())
                        {
                            builder.AllowAnyOrigin();
                            return;
                        }

                        builder.WithOrigins(allowedOrigins);
                    });
                })
                .Configure<MvcOptions>(options =>
                    options.Filters.Add(new CorsAuthorizationFilterFactory(Startup.CorsPolicyName)));

            services.AddDistributedRedisCache(options =>
                this.configuration.GetSection(Startup.RedisCacheOptions).Bind(options));

            if (this.environment.IsDevelopment())
            {
                services.AddMvcCore()
                    .AddApiExplorer();

                services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc(Startup.SwaggerDocVersion, new Info
                    {
                        Title = Startup.SwaggerDocDescription,
                        Version = Startup.SwaggerDocVersion
                    });
                });
            }

            services.Configure<DocumentDbContextOptions>(options =>
            {
                this.configuration.GetSection(Startup.DbContextOptions)
                    .Bind(options);
            });

            services.Configure<FileStoreOptions>(options =>
            {
                if (this.environment.IsDevelopment() || this.environment.IsLocalDevelopment())
                {
                    options.ImagePath = Path.Combine(this.environment.ContentRootPath, "Files");
                    return;
                }

                this.configuration.GetSection(nameof(FileStoreOptions))
                    .Bind(options);
            });

            services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Optimal);

            services.AddResponseCompression();
        }

        public void Configure(IApplicationBuilder app)
        {
            if (this.environment.IsDevelopment() || this.environment.IsLocalDevelopment())
            {
                app.UseSwagger();

                app.UseSwaggerUI(action =>
                {
                    action.SwaggerEndpoint(Startup.SwaggerDocJsonUri, Startup.SwaggerDocDescription);
                });
            }
            else
            {
                app.UseHsts();
            }

            if (!this.environment.IsDevelopment())
            {
                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });
            }

            app.UseIdentityServer();

            app.UseMvc(routes =>
            {
                //var template =
                //    $"api/{nameof(UsersController).Replace("Controller", "").ToLowerInvariant()}/{{action={RouteNames.UserProfileImageUrlRoute}}}/{{id:Guid}}/profileimage";

                //routes.MapRoute(RouteNames.UserProfileImageUrlRoute, template);

                //UserProfileImageUrlFactory.Router = routes.Build();
            });
        }

        public void ConfigureContainer(ContainerBuilder containerBuilder)
        {
            new AutofacFluentBuilder(containerBuilder
                    .AddMediatR(typeof(UserRegisterCommandHandler).Assembly)
                    .AddAutoMapper(typeof(UsersProfile).Assembly))
                .RegisterInstance<Microsoft.Extensions.Hosting.IHostingEnvironment>(this.environment)
                .RegisterInstance<IConfiguration>(this.configuration)
                .RegisterTypeAsSingleton<UserProfileImageUrlFactory, IUserProfileImageUrlFactory>()
                .RegisterTypeAsSingleton<ActionContextAccessor, IActionContextAccessor>()
                .RegisterTypeAsSingleton<Hasher, IHasher>()
                .RegisterTypeAsSingleton<FileService, IFileService>()
                .RegisterTypeAsSingleton<CorsPolicyService, ICorsPolicyService>()
                .RegisterTypeAsScoped<Bus, IBus>()
                .RegisterTypeAsScoped<HttpContextAccessor, IHttpContextAccessor>()
                .RegisterTypeAsScoped<AuthService, IResourceOwnerPasswordValidator>()
                .RegisterTypeAsScoped<AuthService, IProfileService>()
                .RegisterTypeAsScoped<CachedGrantStoreService, IPersistedGrantStore>()
                .RegisterTypeAsScoped<UsersService, IUsersService>()
                .RegisterTypeAsScoped<UserServiceDbContext, DocumentDbContext>()
                .AddClosedTypeAsScoped(typeof(ICommandValidation<>),
                    new[] {typeof(UserRegisterCommandHandler).Assembly})
                .AddClosedTypeAsScoped(typeof(IDocumentRepository<,>), new[] {typeof(UserServiceDbContext).Assembly});
        }
    }
}