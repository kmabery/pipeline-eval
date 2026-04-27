using Microsoft.Extensions.Logging;

namespace PipelineEval.SampleWinFormsApp;

internal sealed class MainForm : Form
{
    private readonly ILogger<MainForm> _logger;

    public MainForm(ILogger<MainForm> logger)
    {
        _logger = logger;
        Text = "PipelineEval observability sample";
        var button = new Button { Text = "Emit log", Dock = DockStyle.Fill };
        button.Click += (_, _) =>
        {
            _logger.LogInformation("WinForms sample button clicked ({Subsystem})", OtelMeterNames.SubsystemTag);
        };
        Controls.Add(button);
    }
}
