namespace VOIDTUNE.WinUI.Services;

/// <summary>A labelled, colour-coded measurement used by the Latency and Benchmark stat grids.</summary>
public sealed record Metric(string Label, string Value, string Hex = "#A78BFA");
