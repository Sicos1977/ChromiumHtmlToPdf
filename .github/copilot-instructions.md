# Copilot Instructions for ChromiumHtmlToPdf

## Repository Overview

ChromiumHtmlToPdf is a 100% managed C# library (targeting `netstandard2.0` and `netstandard2.1`) and a .NET 8 console application that converts HTML to PDF or PNG using a headless Chromium browser (Google Chrome or Microsoft Edge). It communicates with the browser via the Chrome DevTools Protocol (CDP) over a WebSocket connection.

## Solution Structure

```
ChromiumHtmlToPdf.sln
├── ChromiumHtmlToPdfLib/       # Core library (NuGet package: ChromeHtmlToPdf)
│   ├── Converter.cs            # Main entry point – wraps Chromium headless
│   ├── Browser.cs              # Launches and manages the Chromium process
│   ├── Connection.cs           # WebSocket connection to Chrome DevTools Protocol
│   ├── ConvertUri.cs           # Represents a URI/file to convert
│   ├── Enums/                  # Browser, PaperFormats, WindowSize enums
│   ├── Event/                  # CDP event models
│   ├── Exceptions/             # Custom exception types
│   ├── FileCache/              # File-caching helpers
│   ├── Helpers/                # ChromeFinder, EdgeFinder, DocumentHelper, etc.
│   ├── Loggers/                # Logger wrappers
│   ├── Protocol/               # CDP request/response message models
│   └── Settings/               # PageSettings (margins, paper size, orientation…)
└── ChromiumHtmlToPdfConsole/   # .NET 8 CLI wrapper around the library
    ├── Program.cs              # Entry point; uses CommandLineParser for options
    ├── Options.cs              # CLI option definitions
    ├── ConversionItem.cs       # Represents a single conversion job
    ├── FileManager.cs          # Input-file/output-file management
    └── LimitedConcurrencyLevel.cs  # Task scheduler for parallel conversions
```

## Building the Project

```bash
dotnet restore
dotnet build ChromiumHtmlToPdf.sln
```

There are no automated test projects in this repository.

## Key Design Points

- **`Converter`** is the primary public API. Instantiate it, optionally pass an `ILogger`, then call `ConvertToPdfAsync` / `ConvertToPngAsync` (or their synchronous variants).
- **`PageSettings`** controls paper size, margins, orientation, background printing, and scale.
- **`Browser`** finds the Chromium executable (via `ChromeFinder` / `EdgeFinder`), starts it with `--headless`, `--remote-debugging-port`, etc., and exposes the WebSocket endpoint.
- **`Connection`** wraps a `ClientWebSocket` and handles CDP message serialisation with `Newtonsoft.Json`.
- The library uses `Microsoft.Extensions.Logging.Abstractions` for logging; callers supply their own `ILogger` (or omit it to suppress logging).
- `HtmlSanitizer` is used to sanitise untrusted HTML before conversion.
- `AngleSharp.Io` is used for HTML parsing tasks such as image validation and pre-wrapping.

## Coding Conventions

- **Language version**: `latest` (C# 12+).
- **Nullable reference types**: enabled (`<Nullable>enable</Nullable>`).
- Every source file starts with the standard MIT licence header block (see any existing `.cs` file for the exact wording).
- Use `async`/`await` throughout; public async methods should expose both async and synchronous (blocking) overloads.
- XML doc comments (`/// <summary>…`) are required on all public members; the project generates an XML documentation file.
- Follow existing patterns for error handling: throw typed exceptions from `Exceptions/` rather than bare `Exception`.
- Keep Chromium-process management inside `Browser.cs`; keep CDP message logic inside `Connection.cs`.
- Console app options are parsed with `CommandLineParser`; add new options to `Options.cs` and handle them in `Program.cs`.
