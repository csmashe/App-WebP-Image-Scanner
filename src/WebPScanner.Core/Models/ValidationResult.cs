namespace WebPScanner.Core.Models;

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indicates whether the validation passed.
    /// </summary>
    public bool IsValid { get; private init; }

    /// <summary>
    /// Collection of error messages when validation fails.
    /// </summary>
    public List<string> Errors { get; private init; } = [];

    /// <summary>
    /// Creates a successful validation result.
    /// </summary>
    /// <returns>A validation result indicating success.</returns>
    public static ValidationResult Success() => new() { IsValid = true };

    /// <summary>
    /// Creates a failed validation result with a single error.
    /// </summary>
    /// <param name="error">The error message.</param>
    /// <returns>A validation result indicating failure.</returns>
    public static ValidationResult Failure(string error) => new()
    {
        IsValid = false,
        Errors = [error]
    };

    /// <summary>
    /// Creates a failed validation result with multiple errors.
    /// </summary>
    /// <param name="errors">The collection of error messages.</param>
    /// <returns>A validation result indicating failure.</returns>
    public static ValidationResult Failure(IEnumerable<string> errors) => new()
    {
        IsValid = false,
        Errors = errors.ToList()
    };
}
