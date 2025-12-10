using System.ComponentModel.DataAnnotations;

namespace OmniRAG.Core.Configuration;

/// <summary>
/// Python runtime configuration.
/// </summary>
public class PythonOptions
{
    [Required(ErrorMessage = "Python DLL path is required")]
    public string DllPath { get; set; } = string.Empty;

    [Required(ErrorMessage = "Python home path is required")]
    public string HomePath { get; set; } = string.Empty;
}
