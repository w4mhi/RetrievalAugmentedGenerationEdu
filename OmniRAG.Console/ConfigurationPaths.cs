namespace OmniRAG.Console;

/// <summary>
/// Configuration paths container for service registration.
/// Reduces parameter count in helper methods (Rule 16).
/// </summary>
internal class ConfigurationPaths
{
    public required string PythonDll { get; init; }
    public required string PythonHome { get; init; }
    public required string ChromaDir { get; init; }
    public required string PdfDirectory { get; init; }
}
