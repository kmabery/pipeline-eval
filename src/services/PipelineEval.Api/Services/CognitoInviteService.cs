using System.Diagnostics.Metrics;
using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.Runtime;
using PipelineEval.Api.Meters;

namespace PipelineEval.Api.Services;

/// <summary>
/// Invites a user by email. Mirrors the <c>nextService</c> CLI template's interface/impl service split so the
/// Map* extensions can depend on an abstraction rather than the concrete Cognito client.
/// </summary>
public interface IInviteService
{
    Task InviteUserByEmailAsync(string email, CancellationToken cancellationToken);
}

public sealed class CognitoInviteService : IInviteService, IDisposable
{
    private readonly IAmazonCognitoIdentityProvider _cognito;
    private readonly IConfiguration _configuration;
    private readonly Meter _meter;
    private readonly Counter<long> _inviteCreated;

    public CognitoInviteService(
        IAmazonCognitoIdentityProvider cognito,
        IConfiguration configuration,
        IMeterFactory meterFactory)
    {
        _cognito = cognito;
        _configuration = configuration;
        _meter = meterFactory.Create(PipelineEvalApiMeterNames.MeterName);
        _inviteCreated = _meter.CreateCounter<long>(PipelineEvalApiMeterNames.InviteCreated);
    }

    public async Task InviteUserByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var poolId = _configuration["Cognito:UserPoolId"]
            ?? throw new InvalidOperationException("Cognito:UserPoolId is not configured.");

        email = email.Trim();
        try
        {
            await _cognito.AdminCreateUserAsync(
                new AdminCreateUserRequest
                {
                    UserPoolId = poolId,
                    Username = email,
                    UserAttributes =
                    [
                        new AttributeType { Name = "email", Value = email },
                    ],
                    DesiredDeliveryMediums = ["EMAIL"],
                },
                cancellationToken).ConfigureAwait(false);
            _inviteCreated.Add(1);
        }
        catch (UsernameExistsException)
        {
            throw new InvalidOperationException("A user with that email already exists.");
        }
    }

    public void Dispose() => _meter.Dispose();
}

public static class CognitoClientExtensions
{
    public static void AddCognitoIdentityProvider(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IAmazonCognitoIdentityProvider>(_ =>
        {
            var regionName = configuration["Cognito:Region"] ?? "us-east-1";
            var region = RegionEndpoint.GetBySystemName(regionName);
            var serviceUrl = configuration["Cognito:ServiceUrl"];
            if (string.IsNullOrWhiteSpace(serviceUrl))
                return new AmazonCognitoIdentityProviderClient(region);

            // LocalStack and similar: anonymous credentials; signing region must match pool region.
            var creds = new BasicAWSCredentials(
                configuration["Cognito:LocalStackAccessKeyId"] ?? "test",
                configuration["Cognito:LocalStackSecretKey"] ?? "test");
            var cfg = new AmazonCognitoIdentityProviderConfig
            {
                ServiceURL = serviceUrl.TrimEnd('/'),
                AuthenticationRegion = regionName,
            };
            return new AmazonCognitoIdentityProviderClient(creds, cfg);
        });

        services.AddSingleton<CognitoInviteService>();
        services.AddSingleton<IInviteService>(sp => sp.GetRequiredService<CognitoInviteService>());
    }
}
