using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using PlusUltra.Swagger.Filters;
using Swashbuckle.AspNetCore.Filters;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace PlusUltra.Swagger.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddDocumentation(this IServiceCollection services, Info info, bool userFluentValidators = false, Action<SwaggerGenOptions> configuration = null)
        {
            services.AddSwaggerGen(
                options =>
                {
                    options.DescribeAllEnumsAsStrings();
                    options.DescribeStringEnumsInCamelCase();
                    options.DescribeAllParametersInCamelCase();

                    // resolve the IApiVersionDescriptionProvider service
                    // note: that we have to build a temporary service provider here because one has not been created yet
                    var provider = services.BuildServiceProvider().GetRequiredService<IApiVersionDescriptionProvider>();

                    // add a swagger document for each discovered API version
                    // note: you might choose to skip or document deprecated API versions differently
                    foreach (var description in provider.ApiVersionDescriptions)
                    {
                        options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(info, description));
                    }

                    if (userFluentValidators)
                        options.AddFluentValidationRules();

                    // integrate xml comments
                    IncludeXMLS(options);

                    options.OperationFilter<AddResponseHeadersFilter>(); 
                    options.OperationFilter<AuthResponsesOperationFilter>();
                    
                    configuration?.Invoke(options);
                });

            return services;
        }

        public static IApplicationBuilder UseDocumentation(this IApplicationBuilder app, IApiVersionDescriptionProvider provider, Action<SwaggerUIOptions> configuration = null)
        {
            // Enable middleware to serve generated Swagger as a JSON endpoint.
            app.UseSwagger();

            // Enable middleware to serve swagger-ui (HTML, JS, CSS, etc.), 
            // specifying the Swagger JSON endpoint.
            app.UseSwaggerUI(c =>
            {
                // build a swagger endpoint for each discovered API version
                foreach (var description in provider.ApiVersionDescriptions)
                {
                    c.SwaggerEndpoint($"./swagger/{description.GroupName}/swagger.json", description.GroupName.ToUpperInvariant());
                    c.RoutePrefix = string.Empty;
                }

                configuration?.Invoke(c);
            });

            return app;
        }

        static Info CreateInfoForApiVersion(Info info, ApiVersionDescription description)
        {
            info.Version = description.ApiVersion.ToString();

            if (description.IsDeprecated)
            {
                info.Description += " This API version has been deprecated.";
            }

            return info;
        }

        private static void IncludeXMLS(SwaggerGenOptions options)
        {
            var app = PlatformServices.Default.Application;
            var path = app.ApplicationBasePath;

            var files = Directory.GetFiles(path, "*.xml");
            foreach (var item in files)
                options.IncludeXmlComments(item);

        }
    }
}