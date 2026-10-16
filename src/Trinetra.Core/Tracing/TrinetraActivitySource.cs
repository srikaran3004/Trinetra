using System.Diagnostics;

namespace Trinetra.Core.Tracing;

public static class TrinetraActivitySource
{
    public const string SourceName = "Trinetra.Agents";
    public const string Version = "1.0.0";

    public static readonly ActivitySource Source = new(SourceName, Version);
}
