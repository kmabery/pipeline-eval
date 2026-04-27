using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.Extensions.Configuration.SystemsManager;
using Amazon.SimpleSystemsManagement;
using Amazon.SimpleSystemsManagement.Model;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Primitives;
using PipelineEval.Observability;

CoralogixEnvConfiguration.LoadDotEnvIfPresent();
var builder = WebApplication.CreateBuilder(args);

var ssmPrefix =
    builder.Configuration["Aws:SsmParameterPrefix"]
    ?? Environment.GetEnvironmentVariable("AWS_SSM_PARAMETER_PREFIX");
if (!string.IsNullOrWhiteSpace(ssmPrefix))
{
    builder.Configuration.AddSystemsManager(
        configureSource =>
        {
            configureSource.Path = ssmPrefix.TrimEnd('/');
            configureSource.Optional = true;
            configureSource.ReloadAfter = TimeSpan.FromMinutes(5);
        });
}

CoralogixEnvConfiguration.ApplyCoralogixEnvironment(builder.Configuration);
builder.AddPipelineEvalObservability();

var ssmParameterName = builder.Configuration["Gateway:SsmReverseProxyParameter"];
if (!string.IsNullOrWhiteSpace(ssmParameterName))
{
    var json = await LoadSsmParameterJsonAsync(ssmParameterName, CancellationToken.None);
    if (!string.IsNullOrWhiteSpace(json))
    {
        var wrapped = WrapReverseProxyJson(json);
        builder.Configuration.AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(wrapped)));
    }
}

builder.Services.AddHealthChecks();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();
app.UseForwardedHeaders();
app.MapHealthChecks("/health");
app.MapReverseProxy();

await app.RunAsync();

static string WrapReverseProxyJson(string json)
{
    var trimmed = json.TrimStart();
    if (trimmed.StartsWith('{') && trimmed.Contains("\"ReverseProxy\"", StringComparison.Ordinal))
        return trimmed;

    var node = JsonNode.Parse(json);
    if (node is null)
        throw new InvalidOperationException("Gateway: SSM reverse-proxy JSON could not be parsed.");

    var root = new JsonObject { ["ReverseProxy"] = node };
    return root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
}

static async Task<string?> LoadSsmParameterJsonAsync(string parameterName, CancellationToken cancellationToken)
{
    using var client = new AmazonSimpleSystemsManagementClient();
    try
    {
        var response = await client.GetParameterAsync(new GetParameterRequest
        {
            Name = parameterName,
            WithDecryption = true,
        }, cancellationToken).ConfigureAwait(false);

        return response.Parameter?.Value;
    }
    catch (Amazon.SimpleSystemsManagement.Model.ParameterNotFoundException)
    {
        return null;
    }
}
