namespace GlobalExceptionHandling.Api.Exceptions;

/// <summary>
/// Thrown when incoming request data fails validation.
/// Maps to HTTP 400 Bad Request.
/// </summary>
public sealed class ValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidationException"/> with a collection of validation errors.
    /// </summary>
    /// <param name="errors">Dictionary of field-name to error-message pairs.</param>
    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    /// <summary>Gets the validation errors keyed by field name.</summary>
    public IDictionary<string, string[]> Errors { get; }
}
