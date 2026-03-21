# Global Exception Handling in ASP.NET Core (.NET 10)

[![Visit CodingDroplets](https://img.shields.io/badge/Website-codingdroplets.com-blue?style=for-the-badge&logo=google-chrome&logoColor=white)](https://codingdroplets.com/)
[![YouTube](https://img.shields.io/badge/YouTube-CodingDroplets-red?style=for-the-badge&logo=youtube&logoColor=white)](https://www.youtube.com/@CodingDroplets)
[![Patreon](https://img.shields.io/badge/Patreon-Support%20Us-orange?style=for-the-badge&logo=patreon&logoColor=white)](https://www.patreon.com/CodingDroplets)
[![GitHub](https://img.shields.io/badge/GitHub-codingdroplets-black?style=for-the-badge&logo=github&logoColor=white)](http://github.com/codingdroplets/)

A production-ready code sample demonstrating **centralized global exception handling** in ASP.NET Core using the built-in `IExceptionHandler` interface and **RFC 7807 Problem Details** responses.

No more scattered try/catch blocks spread across every controller. One handler to rule them all.

---

## 🚀 Support the Channel — Join on Patreon

If this sample saved you time, consider joining our Patreon community.
You'll get **exclusive .NET tutorials, premium code samples, and early access** to new content — all for the price of a coffee.

👉 **[Join CodingDroplets on Patreon](https://www.patreon.com/CodingDroplets)**

---

## What You'll Learn

- How to implement `IExceptionHandler` for centralized error handling
- How to return structured **Problem Details** (RFC 7807) responses
- How to map custom exception types to specific HTTP status codes
- How to keep internal error details out of API responses (security best practice)
- How to write integration tests that verify exception handling behavior

---

## Project Structure

```
dotnet-global-exception-handling/
├── src/
│   └── GlobalExceptionHandling.Api/
│       ├── Controllers/
│       │   └── ProductsController.cs      # Demo controller with intentional error triggers
│       ├── Exceptions/
│       │   ├── NotFoundException.cs       # Maps to HTTP 404
│       │   ├── ValidationException.cs     # Maps to HTTP 400 with field errors
│       │   └── ForbiddenException.cs      # Maps to HTTP 403
│       ├── Handlers/
│       │   └── GlobalExceptionHandler.cs  # The single IExceptionHandler implementation
│       └── Program.cs                     # App setup and middleware registration
└── tests/
    └── GlobalExceptionHandling.Tests/
        ├── GlobalExceptionHandlerTests.cs # Integration tests for each HTTP status code
        └── ExceptionTests.cs              # Unit tests for custom exception types
```

---

## How It Works

### 1. Define Custom Exception Types

Each exception type carries semantic meaning and maps to an HTTP status code:

```csharp
// Maps to 404 Not Found
throw new NotFoundException("Product", id);

// Maps to 400 Bad Request with field-level errors
throw new ValidationException(new Dictionary<string, string[]>
{
    { "Name", ["Product name is required."] }
});

// Maps to 403 Forbidden
throw new ForbiddenException("Only administrators can delete products.");
```

### 2. Register the Global Exception Handler

In `Program.cs`, two lines are all it takes:

```csharp
// Register the handler (must be before app.Build())
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Wire it into the middleware pipeline (must be first!)
app.UseExceptionHandler();
```

### 3. Implement IExceptionHandler

The `GlobalExceptionHandler` uses a pattern-matching switch to route each exception type to the correct HTTP response:

```csharp
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            NotFoundException notFound =>
                (404, "Not Found", notFound.Message),

            ValidationException =>
                (400, "Validation Failed", "One or more validation errors occurred."),

            ForbiddenException forbidden =>
                (403, "Forbidden", forbidden.Message),

            // Never leak internal exception details to the client
            _ => (500, "Internal Server Error", "An unexpected error occurred.")
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsync(/* Problem Details JSON */);
        return true; // Handled — stop the pipeline
    }
}
```

### 4. Structured Problem Details Response (RFC 7807)

Every error response follows the same predictable shape:

```json
{
  "status": 404,
  "title": "Not Found",
  "detail": "Resource 'Product' with id '9999' was not found.",
  "instance": "/api/products/9999"
}
```

For validation errors, field-level details are included:

```json
{
  "status": 400,
  "title": "Validation Failed",
  "detail": "One or more validation errors occurred.",
  "errors": {
    "Name": ["Product name is required."]
  },
  "instance": "/api/products"
}
```

---

## Running the Sample

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Clone & Run

```bash
git clone https://github.com/codingdroplets/dotnet-global-exception-handling.git
cd dotnet-global-exception-handling

dotnet run --project src/GlobalExceptionHandling.Api
```

Navigate to **http://localhost:5289/swagger** to open Swagger UI and test all endpoints interactively.

### Run Tests

```bash
dotnet test -c Release
```

---

## Endpoints & Expected Responses

| Method   | Endpoint                  | Exception Thrown       | HTTP Status |
|----------|---------------------------|------------------------|-------------|
| `GET`    | `/api/products`           | _(none)_               | `200 OK`    |
| `GET`    | `/api/products/{id}`      | `NotFoundException`    | `404`       |
| `POST`   | `/api/products`           | `ValidationException`  | `400`       |
| `DELETE` | `/api/products/{id}`      | `ForbiddenException`   | `403`       |
| `GET`    | `/api/products/crash`     | `InvalidOperationException` | `500`  |

Try hitting `/api/products/9999` — you'll get a clean 404 Problem Details response with no stack trace leaked.

---

## Key Concepts

### Why IExceptionHandler over Custom Middleware?

`IExceptionHandler` (introduced in .NET 8) is the **official Microsoft-recommended pattern**:

- **Chainable:** You can register multiple handlers and chain them
- **Integrated:** Works seamlessly with `AddProblemDetails()` and the existing exception handling pipeline
- **Testable:** Easily unit-tested without mocking middleware directly

### Why Problem Details (RFC 7807)?

Problem Details is the standard format for HTTP API errors:
- **Consistent shape** across all error types
- **Machine-readable** — clients can parse and act on it
- **Human-readable** — developers can understand it at a glance
- **Widely supported** by API clients, monitoring tools, and documentation generators

### Security: Never Leak Internal Errors

The catch-all branch intentionally returns a generic message:

```csharp
_ => (500, "Internal Server Error", "An unexpected error occurred. Please try again later.")
```

The actual exception (with stack trace) is **logged server-side** but **never returned to the client**.

---

## Technologies Used

- **.NET 10** / **ASP.NET Core 10**
- **IExceptionHandler** (built-in, no extra packages needed)
- **Problem Details** (RFC 7807, `AddProblemDetails()`)
- **Swashbuckle** (Swagger UI)
- **xUnit** + **Microsoft.AspNetCore.Mvc.Testing** (integration tests)

---

## License

MIT — free to use, modify, and share.

---

## 🔗 Connect with CodingDroplets

| Platform | Link |
|----------|------|
| 🌐 Website | https://codingdroplets.com/ |
| 📺 YouTube | https://www.youtube.com/@CodingDroplets |
| 🎁 Patreon | https://www.patreon.com/CodingDroplets |
| 💻 GitHub | http://github.com/codingdroplets/ |

> **Want more samples like this?** [Support us on Patreon](https://www.patreon.com/CodingDroplets) and get access to premium .NET content.
