﻿using System.Linq;
using System.Reflection;
using Autofac;
using AutoMapper;
using Etdb.ServiceBase.Builder.Builder;
using Etdb.ServiceBase.Constants;
using Etdb.ServiceBase.General.Abstractions.Hasher;
using Etdb.ServiceBase.General.Hasher;
using Etdb.UserService.Application.Config;
using Etdb.UserService.Application.ExceptionFilter;
using Etdb.UserService.Application.Services;
using Etdb.UserService.Application.Validators;
using Etdb.UserService.Data;
using Etdb.UserService.EventStore;
using IdentityServer4.Services;
using IdentityServer4.Validation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Cors.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Swashbuckle.AspNetCore.Swagger;

namespace Etdb.UserService.Bootstrap
{
    public class Startup
    {
        private readonly IHostingEnvironment hostingEnvironment;
        private readonly IConfigurationRoot configurationRoot;

        private const string SwaggerDocDescription = "ETDB " + ServiceNames.UserService + " V1";
        private const string SwaggerDocVersion = "v1";
        private const string SwaggerDocJsonUri = "/swagger/v1/swagger.json";

        private const string CorsPolicyName = "AllowAll";

        private const string AuthenticationSchema = "Bearer";

        private const string ApplicationAssemblyPrefix = "Etdb.UserService";

        private const string MediatorAssemblyPrefix = "Etdb.ServiceBase";

        private readonly Assembly[] applicatiAssemblies;

        private readonly Assembly[] mediatorAssemblies;

        public Startup(IHostingEnvironment hostingEnvironment)
        {
            this.hostingEnvironment = hostingEnvironment;

            var builder = new ConfigurationBuilder()
                .SetBasePath(hostingEnvironment.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{hostingEnvironment.EnvironmentName}.json", true)
                .AddEnvironmentVariables();

            this.configurationRoot = builder.Build();

            this.applicatiAssemblies = DependencyContext
                .Default
                .CompileLibraries
                .SelectMany(lib => lib.Assemblies)
                .Where(assemblyName => assemblyName.StartsWith(Startup.ApplicationAssemblyPrefix))
                .Select(assemblyName => Assembly.Load(assemblyName.Replace(".dll", "")))
                .ToArray();

            this.mediatorAssemblies = DependencyContext
                .Default
                .CompileLibraries
                .SelectMany(lib => lib.Assemblies)
                .Where(assemblyName => assemblyName.StartsWith(Startup.MediatorAssemblyPrefix))
                .Select(assemblyName => Assembly.Load(assemblyName.Replace(".dll", "")))
                .ToArray();
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                options.Filters.Add(new AuthorizeFilter(new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build()));

                options.Filters.Add(new DbUpdateExceptionFilter());
            }).AddJsonOptions(options =>
            {
                options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            });

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

            services.AddIdentityServer()
                .AddDeveloperSigningCredential()
                .AddInMemoryIdentityResources(new IdentityResourceConfig().GetIdentityResource())
                .AddInMemoryApiResources(new ApiResourceConfig().GetApiResource())
                .AddInMemoryClients(new ClientConfig().GetClients(this.configurationRoot));

            services.AddAuthentication(Startup.AuthenticationSchema)
                .AddIdentityServerAuthentication(options =>
                {
                    options.Authority = "http://localhost:5000";
                    options.RequireHttpsMetadata = false;
                    options.ApiName = ServiceNames.UserService;
                });

            services.AddCors(options =>
            {
                options.AddPolicy(Startup.CorsPolicyName, opt =>
                {
                    opt.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            })
            .Configure<MvcOptions>(options => 
                options.Filters.Add(new CorsAuthorizationFilterFactory(Startup.CorsPolicyName)));

            services.AddMediatR(this.applicatiAssemblies);

            services.AddAutoMapper(this.applicatiAssemblies);
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (!env.IsProduction())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }

            app.UseSwagger();
            app.UseSwaggerUI(action =>
            {
                action.SwaggerEndpoint(Startup.SwaggerDocJsonUri, Startup.SwaggerDocDescription);
            });

            app.UseIdentityServer();
            app.UseMvc();
        }

        public void ConfigureContainer(ContainerBuilder containerBuilder)
        {
            new ServiceContainerBuilder(containerBuilder)
                .UseEventSourcing<UserServiceContext, EventStoreContext>(this.applicatiAssemblies)
                .UseGenericRepositoryPattern<UserServiceContext>(this.applicatiAssemblies)
                .UseEnvironment(this.hostingEnvironment)
                .UseConfiguration(this.configurationRoot)
                .RegisterTypePerDependency<Hasher, IHasher>()
                .RegisterTypePerLifetimeScope<ResourceOwnerPasswordValidator, IResourceOwnerPasswordValidator>()
                .RegisterTypePerLifetimeScope<ProfileService, IProfileService>();
        }
    }
}