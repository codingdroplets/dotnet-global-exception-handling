namespace GlobalExceptionHandling.Api.Exceptions;

/// <summary>
/// Thrown when the current user does not have permission to perform an action.
/// Maps to HTTP 403 Forbidden.
/// </summary>
public sealed class ForbiddenException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="ForbiddenException"/>.
    /// </summary>
    public ForbiddenException()
        : base("You do not have permission to perform this action.")
    {
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ForbiddenException"/> with a custom message.
    /// </summary>
    /// <param name="message">The message describing the reason for the exception.</param>
    public ForbiddenException(string message)
        : base(message)
    {
    }
}
