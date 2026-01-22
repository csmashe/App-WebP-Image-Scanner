namespace WebPScanner.Core.DTOs;

/// <summary>
/// Data transfer object for validation error responses.
/// </summary>
public class ValidationErrorDto
{
    /// <summary>
    /// Indicates the request was invalid. Always false for validation errors.
    /// </summary>
    // ReSharper disable once MemberCanBeMadeStatic.Global - Instance property required for JSON serialization
#pragma warning disable CA1822
    public bool Success => false;
#pragma warning restore CA1822

    /// <summary>
    /// List of validation error messages.
    /// </summary>
    public List<string> Errors { get; init; } = [];

    public static ValidationErrorDto FromErrors(IEnumerable<string> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return new ValidationErrorDto { Errors = errors.ToList() };
    }

    public static ValidationErrorDto FromError(string error) => new()
    {
        Errors = [error]
    };
}
