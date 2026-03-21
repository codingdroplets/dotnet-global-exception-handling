using GlobalExceptionHandling.Api.Exceptions;

namespace GlobalExceptionHandling.Tests;

/// <summary>
/// Unit tests for the custom exception types.
/// </summary>
public class ExceptionTests
{
    [Fact]
    public void NotFoundException_SetsMessageCorrectly()
    {
        var ex = new NotFoundException("Product", 42);

        Assert.Equal("Resource 'Product' with id '42' was not found.", ex.Message);
        Assert.Equal("Product", ex.ResourceName);
        Assert.Equal(42, ex.ResourceId);
    }

    [Fact]
    public void ValidationException_SetsErrorsCorrectly()
    {
        var errors = new Dictionary<string, string[]>
        {
            { "Name", ["Name is required."] }
        };

        var ex = new ValidationException(errors);

        Assert.Equal("One or more validation errors occurred.", ex.Message);
        Assert.Single(ex.Errors);
        Assert.Equal(["Name is required."], ex.Errors["Name"]);
    }

    [Fact]
    public void ForbiddenException_DefaultMessage()
    {
        var ex = new ForbiddenException();

        Assert.Equal("You do not have permission to perform this action.", ex.Message);
    }

    [Fact]
    public void ForbiddenException_CustomMessage()
    {
        var ex = new ForbiddenException("Admin only.");

        Assert.Equal("Admin only.", ex.Message);
    }
}
