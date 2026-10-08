using Nwn.Authoring.Areas.Generation.Hosting;
using Serilog;

namespace SWLOR.Toolset.Domain.AreaGeneration.Hosting;

/// <summary>Routes generator diagnostics to Serilog.</summary>
public sealed class SwlorAreaGenerationLog : IAreaGenerationLog
{
    private static readonly ILogger Logger = Log.ForContext("SourceContext", "SWLOR.Toolset.Domain.AreaGeneration");

    public void Information(string message) => Logger.Information("{Message}", message);

    public void Warning(string message) => Logger.Warning("{Message}", message);

    public void Error(Exception exception, string message) => Logger.Error(exception, "{Message}", message);
}
