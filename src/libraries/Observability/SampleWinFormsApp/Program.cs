using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PipelineEval.Observability;

namespace PipelineEval.SampleWinFormsApp;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        CoralogixEnvConfiguration.LoadDotEnvIfPresent();
        var builder = Host.CreateApplicationBuilder();
        CoralogixEnvConfiguration.ApplyCoralogixEnvironment(builder.Configuration);
        builder.AddPipelineEvalObservability();
        builder.Services.AddTransient<MainForm>();

        using var host = builder.Build();
        Application.Run(host.Services.GetRequiredService<MainForm>());
    }
}
