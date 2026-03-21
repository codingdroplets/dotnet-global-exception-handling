using GlobalExceptionHandling.Api.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace GlobalExceptionHandling.Api.Controllers;

/// <summary>
/// Demo controller that deliberately throws different exception types
/// so you can observe how the global exception handler responds.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json", "application/problem+json")]
public class ProductsController : ControllerBase
{
    // Simulated in-memory product store
    private static readonly Dictionary<int, string> Products = new()
    {
        { 1, "Laptop" },
        { 2, "Keyboard" },
        { 3, "Monitor" }
    };

    /// <summary>
    /// Returns all products.
    /// </summary>
    /// <returns>A list of products.</returns>
    /// <response code="200">Products returned successfully.</response>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<object>), StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        var result = Products.Select(p => new { Id = p.Key, Name = p.Value });
        return Ok(result);
    }

    /// <summary>
    /// Returns a single product by ID.
    /// Throws <see cref="NotFoundException"/> when the product does not exist → 404.
    /// </summary>
    /// <param name="id">The product identifier.</param>
    /// <returns>The product with the given id.</returns>
    /// <response code="200">Product found and returned.</response>
    /// <response code="404">Product not found — triggers NotFoundException → 404 Problem Details.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public IActionResult GetById(int id)
    {
        if (!Products.TryGetValue(id, out var name))
        {
            // GlobalExceptionHandler will convert this into a 404 Problem Details response
            throw new NotFoundException(nameof(Products), id);
        }

        return Ok(new { Id = id, Name = name });
    }

    /// <summary>
    /// Demonstrates a validation failure → 400 Bad Request with field errors.
    /// Send <c>name</c> as an empty string or omit it to trigger the error.
    /// </summary>
    /// <param name="request">Product creation request.</param>
    /// <returns>The created product.</returns>
    /// <response code="201">Product created successfully.</response>
    /// <response code="400">Validation failed — triggers ValidationException → 400 Problem Details.</response>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public IActionResult Create([FromBody] CreateProductRequest request)
    {
        // Manual validation example — in production you'd use FluentValidation or data annotations
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors[nameof(request.Name)] = ["Product name is required."];

        if (request.Name?.Length > 100)
            errors[nameof(request.Name)] = ["Product name must not exceed 100 characters."];

        if (errors.Count > 0)
        {
            // GlobalExceptionHandler will convert this into a 400 ValidationProblemDetails response
            throw new ValidationException(errors);
        }

        var newId = Products.Keys.Max() + 1;
        Products[newId] = request.Name!;

        return CreatedAtAction(nameof(GetById), new { id = newId }, new { Id = newId, Name = request.Name });
    }

    /// <summary>
    /// Demonstrates a forbidden action → 403 Forbidden.
    /// Calling this endpoint always throws <see cref="ForbiddenException"/>.
    /// </summary>
    /// <response code="403">Always forbidden — triggers ForbiddenException → 403 Problem Details.</response>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public IActionResult Delete(int id)
    {
        // Simulate a permission check that always fails for demo purposes
        throw new ForbiddenException("Only administrators can delete products.");
    }

    /// <summary>
    /// Demonstrates an unhandled server error → 500 Internal Server Error.
    /// Calling this endpoint always throws a raw <see cref="Exception"/>.
    /// </summary>
    /// <response code="500">Simulated crash — triggers catch-all → 500 Problem Details.</response>
    [HttpGet("crash")]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public IActionResult SimulateCrash()
    {
        // GlobalExceptionHandler catches this and returns a safe 500 response
        // without leaking stack traces to the client
        throw new InvalidOperationException("Database connection lost — simulated crash.");
    }
}

/// <summary>Request body for creating a product.</summary>
public record CreateProductRequest(string? Name);
