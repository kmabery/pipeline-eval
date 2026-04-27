using System.Security.Claims;

namespace PipelineEval.Api.Authentication;

public static class CurrentUserExtensions
{
    public const string AnonymousSub = "local-anonymous";

    /// <summary>Stable subject for per-user data. Uses JWT <c>sub</c> when auth is required; otherwise <see cref="AnonymousSub"/>.</summary>
    public static string GetUserSub(this HttpContext http, IConfiguration configuration)
    {
        if (!configuration.GetValue("Authentication:RequireAuthentication", true))
            return AnonymousSub;

        var sub = http.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? http.User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(sub))
            throw new InvalidOperationException("Authenticated user missing sub claim.");
        return sub;
    }
}
