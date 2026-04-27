using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace PipelineEval.Api.Authentication;

public static class AuthenticationExtensions
{
    public const string AdminPolicy = "Admin";

    public static IServiceCollection AddPipelineEvalAuthentication(this IServiceCollection services, IConfiguration configuration, IHostEnvironment env)
    {
        var require = configuration.GetValue("Authentication:RequireAuthentication", true);
        if (!require)
            return services;

        var region = configuration["Cognito:Region"] ?? throw new InvalidOperationException("Cognito:Region is required when Authentication:RequireAuthentication is true.");
        var poolId = configuration["Cognito:UserPoolId"] ?? throw new InvalidOperationException("Cognito:UserPoolId is required when Authentication:RequireAuthentication is true.");
        var clientId = configuration["Cognito:ClientId"] ?? throw new InvalidOperationException("Cognito:ClientId is required when Authentication:RequireAuthentication is true.");
        var authority = $"https://cognito-idp.{region}.amazonaws.com/{poolId}";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = authority;
                // Cognito access tokens use claim "client_id"; ID tokens use "aud".
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = true,
                };
                options.RequireHttpsMetadata = !env.IsDevelopment();
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ctx =>
                    {
                        var aud = ctx.Principal?.FindFirst("client_id")?.Value
                            ?? ctx.Principal?.FindFirst("aud")?.Value;
                        if (!string.Equals(aud, clientId, StringComparison.Ordinal))
                            ctx.Fail("Invalid token audience (client).");
                        return Task.CompletedTask;
                    },
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminPolicy, policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireAssertion(ctx => IsAdmin(ctx.User, configuration));
            });
        });

        return services;
    }

    /// <summary>True if the user is in Cognito group <c>admins</c> or their email is listed in <c>Invite:AdminEmails</c>.</summary>
    public static bool IsAdmin(ClaimsPrincipal user, IConfiguration configuration)
    {
        foreach (var claim in user.FindAll("cognito:groups"))
        {
            foreach (var part in claim.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                if (string.Equals(part, "admins", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        var email = user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email");
        if (string.IsNullOrEmpty(email))
            return false;

        var admins = configuration.GetSection("Invite:AdminEmails").Get<string[]>() ?? [];
        return admins.Contains(email, StringComparer.OrdinalIgnoreCase);
    }

    public static IApplicationBuilder UsePipelineEvalAuthentication(this WebApplication app, IConfiguration configuration)
    {
        var require = configuration.GetValue("Authentication:RequireAuthentication", true);
        if (require)
        {
            app.UseAuthentication();
            app.UseAuthorization();
        }

        return app;
    }
}
