using GlobalExceptionHandling.Api.Handlers;

var builder = WebApplication.CreateBuilder(args);

// ── Services ─────────────────────────────────────────────────────────────────

builder.Services.AddControllers();

// Register Swagger / OpenAPI for interactive testing
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Global Exception Handling — ASP.NET Core",
        Version = "v1",
        Description =
            "Demonstrates centralized global exception handling in ASP.NET Core using the " +
            "built-in IExceptionHandler interface and Problem Details (RFC 7807). " +
            "Try the endpoints to see how each exception type maps to a structured HTTP response: " +
            "GET /api/products/{id} → NotFoundException → 404, " +
            "POST /api/products → ValidationException → 400, " +
            "DELETE /api/products/{id} → ForbiddenException → 403, " +
            "GET /api/products/crash → raw Exception → 500."
    });
});

// Register the global exception handler.
// IExceptionHandler is called by UseExceptionHandler() middleware for every unhandled exception.
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Enable Problem Details support (RFC 7807) so the framework can produce
// structured error responses alongside our custom handler.
builder.Services.AddProblemDetails();

var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────

// UseExceptionHandler MUST be first so it catches exceptions from all downstream middleware.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Make Program accessible to integration tests
public partial class Program { }
