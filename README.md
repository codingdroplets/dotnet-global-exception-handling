# Global Exception Handling in ASP.NET Core (.NET 10)

> **Centralized error handling done right** — One `IExceptionHandler` to replace all your scattered try/catch blocks, returning clean **RFC 7807 Problem Details** responses with zero internals leaked.

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-10.0-512BD4)](https://docs.microsoft.com/aspnet/core)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)
[![Visit CodingDroplets](https://img.shields.io/badge/Website-codingdroplets.com-blue?style=flat&logo=google-chrome&logoColor=white)](https://codingdroplets.com/)
[![YouTube](https://img.shields.io/badge/YouTube-CodingDroplets-red?style=flat&logo=youtube&logoColor=white)](https://www.youtube.com/@CodingDroplets)
[![Patreon](https://img.shields.io/badge/Patreon-Support%20Us-orange?style=flat&logo=patreon&logoColor=white)](https://www.patreon.com/CodingDroplets)
[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-Support%20Us-yellow?style=flat&logo=buy-me-a-coffee&logoColor=black)](https://buymeacoffee.com/codingdroplets)
[![GitHub](https://img.shields.io/badge/GitHub-codingdroplets-black?style=flat&logo=github&logoColor=white)](http://github.com/codingdroplets/)

---

## 🚀 Support the Channel — Join on Patreon

If this sample saved you time, consider joining our Patreon community.
You'll get **exclusive .NET tutorials, premium code samples, and early access** to new content — all for the price of a coffee.

👉 **[Join CodingDroplets on Patreon](https://www.patreon.com/CodingDroplets)**

Prefer a one-time tip? [Buy us a coffee ☕](https://buymeacoffee.com/codingdroplets)

---

## 🎯 What You'll Learn

- How to implement **`IExceptionHandler`** — the official Microsoft-recommended pattern since .NET 8
- How to return structured **Problem Details (RFC 7807)** responses for every error type
- How to map custom exception types to specific HTTP status codes (`404`, `400`, `403`, `500`)
- How to **never leak** internal stack traces or exception details to API consumers
- How to write **integration tests** that verify exception handling end-to-end

---

## 🗺️ Architecture Overview

```
Incoming HTTP Request
        │
        ▼
┌───────────────────────────────────────────────────┐
│           ASP.NET Core Middleware Pipeline         │
│  ┌─────────────────────────────────────────────┐  │
│  │      app.UseExceptionHandler()  ← FIRST     │  │
│  │  Catches any unhandled exception downstream │  │
│  └──────────────────┬──────────────────────────┘  │
│                     │                             │
│  ┌──────────────────▼──────────────────────────┐  │
│  │         Controller / Endpoint               │  │
│  │   throws NotFoundException / Validation...  │  │
│  └──────────────────┬──────────────────────────┘  │
└─────────────────────┼─────────────────────────────┘
                      │ exception bubbles up
                      ▼
┌───────────────────────────────────────────────────┐
│          GlobalExceptionHandler                   │
│                                                   │
│  exception switch:                                │
│  ├─ NotFoundException      → 404 Problem Details  │
│  ├─ ValidationException    → 400 Problem Details  │
│  ├─ ForbiddenException     → 403 Problem Details  │
│  └─ anything else          → 500 (no internals)   │
└───────────────────────────────────────────────────┘
                      │
                      ▼
         RFC 7807 Problem Details JSON
```

---

## 📋 Exception → HTTP Status Code Mapping

| Exception Type | HTTP Status | Scenario |
|---|---|---|
| `NotFoundException` | `404 Not Found` | Resource not found by ID |
| `ValidationException` | `400 Bad Request` | Invalid input, field-level errors |
| `ForbiddenException` | `403 Forbidden` | Insufficient permissions |
| Any other exception | `500 Internal Server Error` | Unexpected error (no details leaked) |

---

## 📁 Project Structure

```
dotnet-global-exception-handling/
├── src/
│   └── GlobalExceptionHandling.Api/
│       ├── Controllers/
│       │   └── ProductsController.cs      # Demo controller — intentional error triggers
│       ├── Exceptions/
│       │   ├── NotFoundException.cs       # Maps to HTTP 404
│       │   ├── ValidationException.cs     # Maps to HTTP 400 with field errors
│       │   └── ForbiddenException.cs      # Maps to HTTP 403
│       ├── Handlers/
│       │   └── GlobalExceptionHandler.cs  # Single IExceptionHandler implementation
│       └── Program.cs                     # App setup and middleware registration
└── tests/
    └── GlobalExceptionHandling.Tests/
        ├── GlobalExceptionHandlerTests.cs # Integration tests — all HTTP status codes
        └── ExceptionTests.cs              # Unit tests — custom exception types
```

---

## 🛠️ Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Any IDE: Visual Studio 2022+, VS Code, or JetBrains Rider

---

## ⚡ Quick Start

```bash
# Clone the repo
git clone https://github.com/codingdroplets/dotnet-global-exception-handling.git
cd dotnet-global-exception-handling

# Build
dotnet build -c Release

# Run the API
dotnet run --project src/GlobalExceptionHandling.Api

# Open Swagger UI → http://localhost:5289/swagger
```

---

## 🔧 How It Works

### Step 1 — Define Custom Exception Types

Each exception type carries semantic meaning that maps directly to an HTTP status:

```csharp
// Throws → 404 Not Found
throw new NotFoundException("Product", id);

// Throws → 400 Bad Request with field-level errors
throw new ValidationException(new Dictionary<string, string[]>
{
    { "Name", ["Product name is required."] }
});

// Throws → 403 Forbidden
throw new ForbiddenException("Only administrators can delete products.");
```

### Step 2 — Register in Program.cs (Two Lines)

```csharp
// Register before app.Build()
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Wire into middleware pipeline — must be FIRST
app.UseExceptionHandler();
```

### Step 3 — Implement IExceptionHandler

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

            // Never leak internal details to consumers
            _ => (500, "Internal Server Error", "An unexpected error occurred.")
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsync(/* Problem Details JSON */);
        return true; // Handled — stop the pipeline
    }
}
```

---

## 📡 API Endpoints & Expected Responses

| Method | Endpoint | Exception Thrown | HTTP Status |
|--------|----------|------------------|-------------|
| `GET` | `/api/products` | _(none)_ | `200 OK` |
| `GET` | `/api/products/{id}` | `NotFoundException` | `404` |
| `POST` | `/api/products` | `ValidationException` | `400` |
| `DELETE` | `/api/products/{id}` | `ForbiddenException` | `403` |
| `GET` | `/api/products/crash` | `InvalidOperationException` | `500` |

Try `/api/products/9999` — you'll get a clean 404 with no stack trace in sight.

---

## 📦 Problem Details Response Shape (RFC 7807)

Every error response follows the same predictable structure:

```json
{
  "status": 404,
  "title": "Not Found",
  "detail": "Resource 'Product' with id '9999' was not found.",
  "instance": "/api/products/9999"
}
```

Validation errors include field-level detail:

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

## 🧪 Running Tests

```bash
dotnet test -c Release
```

**11 tests** across two test classes:

| Test Class | Tests | Coverage |
|---|---|---|
| `GlobalExceptionHandlerTests` | 7 integration tests | Each exception type → correct HTTP status + body |
| `ExceptionTests` | 4 unit tests | Custom exception construction + message formatting |

---

## 🤔 Key Concepts

### Why `IExceptionHandler` Over Custom Middleware?

`IExceptionHandler` (introduced in .NET 8) is the **official Microsoft-recommended pattern**:

| Feature | `IExceptionHandler` | Custom Middleware |
|---------|--------------------|--------------------|
| Chainable (multiple handlers) | ✅ | ❌ Manual wiring |
| Integrates with `AddProblemDetails()` | ✅ | ❌ |
| Registered via DI | ✅ | Partially |
| Testable with `WebApplicationFactory` | ✅ | ✅ |

### Why Problem Details (RFC 7807)?

- **Consistent shape** — every error looks the same, regardless of type
- **Machine-readable** — clients can parse and act on `status` + `title`
- **Human-readable** — developers understand it at a glance
- **Industry standard** — supported by API gateways, monitoring tools, and documentation generators

### Security: Never Leak Internal Errors

The catch-all branch returns only a generic message — the real exception with stack trace is **logged server-side** but **never returned to the client**:

```csharp
_ => (500, "Internal Server Error", "An unexpected error occurred. Please try again later.")
```

---

## 🏷️ Technologies Used

- **.NET 10** / **ASP.NET Core 10**
- **`IExceptionHandler`** (built-in — no extra NuGet packages)
- **Problem Details** (RFC 7807, `AddProblemDetails()`)
- **Swashbuckle** (Swagger UI)
- **xUnit** + **`Microsoft.AspNetCore.Mvc.Testing`** (integration tests)

---

## 📚 References

- [Handle errors in ASP.NET Core — Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling)
- [Problem Details for HTTP APIs (RFC 7807)](https://www.rfc-editor.org/rfc/rfc7807)
- [IExceptionHandler Interface — .NET API](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.diagnostics.iexceptionhandler)

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

## 🔗 Connect with CodingDroplets

| Platform | Link |
|----------|------|
| 🌐 Website | https://codingdroplets.com/ |
| 📺 YouTube | https://www.youtube.com/@CodingDroplets |
| 🎁 Patreon | https://www.patreon.com/CodingDroplets |
| ☕ Buy Me a Coffee | https://buymeacoffee.com/codingdroplets |
| 💻 GitHub | http://github.com/codingdroplets/ |

> **Want more samples like this?** [Support us on Patreon](https://www.patreon.com/CodingDroplets) or [buy us a coffee ☕](https://buymeacoffee.com/codingdroplets) — every bit helps keep the content coming!
