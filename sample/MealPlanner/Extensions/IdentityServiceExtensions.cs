using System.Text;
using API.Data;
using API.Entities;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace API.Extensions;

public static class IdentityServiceExtensions
{
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddIdentityCore<ApplicationUser>(opt =>
        {
            opt.Password.RequireDigit = false;
            opt.Password.RequiredLength = 4;
            opt.Password.RequireNonAlphanumeric = false;
            opt.Password.RequireUppercase = false;
            opt.Password.RequireLowercase = false;
        })
        .AddRoles<ApplicationRole>()
        .AddRoleManager<RoleManager<ApplicationRole>>()
        .AddEntityFrameworkStores<DataContext>();
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var key = Encoding.UTF8.GetBytes(config["TokenKey"] ?? throw new ArgumentNullException("TokenKey is not set in configuration."));
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false
                };
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            })
            .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = config["Authentication:Google:ClientId"] ?? throw new ArgumentNullException("ClientId is not set in configuration.");
                options.ClientSecret = config["Authentication:Google:ClientSecret"] ?? throw new ArgumentNullException("ClientSecret is not set in configuration.");
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.CallbackPath = new PathString("/api/account/google-response");

                options.CorrelationCookie.SameSite = SameSiteMode.None;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.SaveTokens = true;

                options.Events = new OAuthEvents
                {
                    OnRedirectToAuthorizationEndpoint = ctx =>
                    {
                        var state = ctx.Properties.Items[".xsrf"];
                        Console.WriteLine($"[Google→Redirect] state = {state}");
                        return Task.CompletedTask;
                    },
                    OnRemoteFailure = ctx =>
                    {
                        Console.WriteLine($"[Google→Failure] {ctx.Failure?.Message}");
                        ctx.HandleResponse();
                        ctx.Response.StatusCode = 400;
                        return ctx.Response.WriteAsync($"OAuth failure: {ctx.Failure?.Message}");
                    }
                };

            });
        services.AddAuthorizationBuilder()
            .AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));

        return services;
    }
}