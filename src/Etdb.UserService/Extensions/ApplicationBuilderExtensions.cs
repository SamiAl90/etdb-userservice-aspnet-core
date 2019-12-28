﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Hosting;

namespace Etdb.UserService.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder SetupSwagger(this IApplicationBuilder app,
            string jsonUri,
            string description)
        {
            return app
                .UseSwagger()
                .UseSwaggerUI(action => action.SwaggerEndpoint(jsonUri, description));
        }

        public static IApplicationBuilder SetupHsts(this IApplicationBuilder app, IWebHostEnvironment environment)
        {
            return environment.IsAnyDevelopment() 
                ? app
                : app.UseHsts()
                    .UseHttpsRedirection();
        }

        public static IApplicationBuilder SetupForwarding(this IApplicationBuilder app, IWebHostEnvironment environment)
        {
            return environment.IsDevelopment() 
                ? app 
                : app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
                });
        }

        public static void UseConfiguredRouting(this IApplicationBuilder app)
        {
            app.UseRouting();

            app.UseEndpoints(builder => builder.MapControllers());
        }
    }
}