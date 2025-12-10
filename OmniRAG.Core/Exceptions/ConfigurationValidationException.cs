namespace OmniRAG.Core.Exceptions;

/// <summary>
/// Exception thrown when configuration validation fails.
/// </summary>
public class ConfigurationValidationException : Exception
{
    public IReadOnlyList<string> ValidationErrors { get; }

    public ConfigurationValidationException(string message, IEnumerable<string> validationErrors)
        : base(message)
    {
        this.ValidationErrors = validationErrors.ToList().AsReadOnly();
    }

    public ConfigurationValidationException(IEnumerable<string> validationErrors)
        : this("Configuration validation failed", validationErrors)
    {
    }

    public override string ToString()
    {
        string baseMessage = base.ToString();
        string errors = string.Join(Environment.NewLine, this.ValidationErrors.Select(e => $"  • {e}"));
        return $"{baseMessage}{Environment.NewLine}{Environment.NewLine}Validation Errors:{Environment.NewLine}{errors}";
    }
}
