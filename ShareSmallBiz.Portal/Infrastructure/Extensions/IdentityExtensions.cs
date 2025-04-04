using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.IdentityModel.Tokens;
using ShareSmallBiz.Portal.Data;
using ShareSmallBiz.Portal.Data.Entities;
using ShareSmallBiz.Portal.Infrastructure.Utilities;

namespace ShareSmallBiz.Portal.Infrastructure.Extensions
{
    public static class IdentityExtensions
    {
        public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddTransient<IEmailSender, SmtpEmailSender>();
            services.AddIdentity<ShareSmallBizUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.SignIn.RequireConfirmedEmail = true;
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            })
                .AddEntityFrameworkStores<ShareSmallBizUserContext>()
                .AddDefaultTokenProviders()
                .AddSignInManager<SignInManager<ShareSmallBizUser>>()
                .AddUserConfirmation<ShareSmallBizUser>()
                .AddDefaultUI();

            services.AddDataProtection()
                    .SetApplicationName("ShareSmallBiz")
                    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\websites\ShareSmallBiz\keys"));

            var jwtSecret = configuration["JwtSettings:Secret"];
            var issuer = configuration["JwtSettings:Issuer"];
            var audience = configuration["JwtSettings:Audience"];
            var key = Encoding.ASCII.GetBytes(jwtSecret);

            services.AddAuthentication(options => { })
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidateAudience = true,
                            ValidateLifetime = true,
                            ValidateIssuerSigningKey = true,
                            ValidIssuer = issuer,
                            ValidAudience = audience,
                            IssuerSigningKey = new SymmetricSecurityKey(key),
                            ClockSkew = TimeSpan.Zero
                        };
                        options.Events = new JwtBearerEvents
                        {
                            OnAuthenticationFailed = context =>
                            {
                                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                                logger.LogError(context.Exception, "JWT Authentication failed.");
                                return Task.CompletedTask;
                            }
                        };
                    });

            services.AddAuthorization();
            return services;
        }
    }
}
