using Microsoft.OpenApi.Models;

namespace ShareSmallBiz.Portal.Infrastructure.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "ShareSmallBiz API",
                    Description = "API documentation for ShareSmallBiz Portal",
                    Contact = new OpenApiContact
                    {
                        Name = "Support",
                        Email = "support@sharesmallbiz.com",
                        Url = new Uri("https://sharesmallbiz.com/")
                    }
                });

                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });
            return services;
        }
    }
}
