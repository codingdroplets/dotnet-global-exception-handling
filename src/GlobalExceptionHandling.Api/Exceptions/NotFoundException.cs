namespace GlobalExceptionHandling.Api.Exceptions;

/// <summary>
/// Thrown when a requested resource is not found.
/// Maps to HTTP 404 Not Found.
/// </summary>
public sealed class NotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of <see cref="NotFoundException"/>.
    /// </summary>
    /// <param name="resourceName">The name of the resource that was not found.</param>
    /// <param name="resourceId">The identifier that was searched for.</param>
    public NotFoundException(string resourceName, object resourceId)
        : base($"Resource '{resourceName}' with id '{resourceId}' was not found.")
    {
        ResourceName = resourceName;
        ResourceId = resourceId;
    }

    /// <summary>Gets the name of the resource that was not found.</summary>
    public string ResourceName { get; }

    /// <summary>Gets the identifier of the resource that was searched for.</summary>
    public object ResourceId { get; }
}
