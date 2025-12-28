using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Core.Configuration;

/// <summary>
/// Python runtime configuration.
/// </summary>
public class PythonOptions
{
    /// <summary>
    /// Gets or sets the path to the Python DLL (e.g., python311.dll on Windows).
    /// </summary>
    [Required(ErrorMessage = "Python DLL path is required")]
    public string DllPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Python home directory path (where Python is installed).
    /// </summary>
    [Required(ErrorMessage = "Python home path is required")]
    public string HomePath { get; set; } = string.Empty;
}
