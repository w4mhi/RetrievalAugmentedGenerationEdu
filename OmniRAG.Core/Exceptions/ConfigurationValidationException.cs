using System;
using System.Collections.Generic;
using System.Linq;

namespace OmniRAG.Core.Exceptions;

/// <summary>
/// Exception thrown when configuration validation fails.
/// </summary>
public class ConfigurationValidationException : Exception
{
    /// <summary>
    /// Gets the list of validation errors that caused the exception.
    /// </summary>
    public IReadOnlyList<string> ValidationErrors { get; }

    /// <summary>
    /// Initializes a new instance of the ConfigurationValidationException class with a message and validation errors.
    /// </summary>
    /// <param name="message">The exception message.</param>
    /// <param name="validationErrors">The validation errors.</param>
    public ConfigurationValidationException(string message, IEnumerable<string> validationErrors)
        : base(message)
    {
        this.ValidationErrors = validationErrors.ToList().AsReadOnly();
    }

    /// <summary>
    /// Initializes a new instance of the ConfigurationValidationException class with validation errors.
    /// </summary>
    /// <param name="validationErrors">The validation errors.</param>
    public ConfigurationValidationException(IEnumerable<string> validationErrors)
        : this("Configuration validation failed", validationErrors)
    {
    }

    /// <summary>
    /// Returns a string representation of the exception including all validation errors.
    /// </summary>
    /// <returns>A formatted string with the exception details and validation errors.</returns>
    public override string ToString()
    {
        string baseMessage = base.ToString();
        string errors = string.Join(Environment.NewLine, this.ValidationErrors.Select(e => $"  • {e}"));
        return $"{baseMessage}{Environment.NewLine}{Environment.NewLine}Validation Errors:{Environment.NewLine}{errors}";
    }
}
