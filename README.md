# ASP.NET Core Azure Storage Helpers

**Simplify Azure Storage Integration in Your .NET Core Project**

Welcome to Azure Storage Helpers, a lightweight and efficient helper library for integrating Azure Blob Storage into your .NET Core applications. This project provides easy-to-use services for listing, uploading, and deleting files, helping you manage your storage effortlessly.

**Key Features**

* **Seamless Azure Blob Storage Integration**: Quickly integrate with Azure Storage using simple service classes.
* **Controller & Minimal API Support**: Use the helpers with both traditional controllers and minimal API endpoints.
* **File Management**: List, upload, and delete files with minimal boilerplate code.
* **Flexible Configuration**: Easily configure your connection string and storage container.

**Why Choose Azure Storage Helpers?**

* **Saves Time**: Avoid repetitive boilerplate code when working with Azure Storage.
* **Flexible**: Works with any .NET Core application.
* **Easy to Maintain**: Clean, maintainable, and well-documented codebase.

**Get Started Today!**

Integrate Azure Storage Helpers into your project and simplify file management in your .NET Core applications.

## Installation

1. Add `IStorageService.cs` and `StorageService.cs` to your project.
2. Install the NuGet package `Azure.Storage.Blobs`.
3. Configure your appsettings.json:

```json
{
  "StorageConnectionString": "YOUR_STORAGE_CONNECTION_STRING"
}
```

4. Register the service in `Program.cs`:

```csharp
builder.Services.AddSingleton<IStorageService, StorageService>();

builder.Services.AddAzureClients(options =>
{
    options.AddBlobServiceClient("YOUR_STORAGE_CONNECTION_STRING");
});
```

## Usage

### List Files (Controller)

```csharp
public class FilesController : ControllerBase
{
    private readonly IStorageService _storageService;

    public FilesController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpGet, Route("v1/list-files")]
    public async Task<ActionResult> ListFiles()
    {
        List<BlobFile> results = await _storageService.ListFilesAsync("folder/folder", "containerName");

        if (results is null)
            return NotFound("No results found!");

        return Ok(results);
    }
}
```

### List Files (Minimal API)

```csharp
public static class FilesEndpoint
{
    public static void MapListEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("v1/list-files", ListFiles);
    }

    private static async Task<IResult> ListFiles([FromServices] IStorageService storageService)
    {
        List<BlobFile> results = await storageService.ListFilesAsync("folder/folder", "containerName");

        if (results is null)
            return Results.NotFound("No results found!");

        return Results.Ok(results);
    }
}
```

### Upload File (Controller)

```csharp
public class UploadController : ControllerBase
{
    private readonly IStorageService _storageService;

    public UploadController(IStorageService storageService)
    {
        _storageService = storageService;
    }

    [HttpPost, Route("v1/upload-file")]
    public async Task<ActionResult> UploadFile(IFormFile file)
    {
        using Stream stream = file.OpenReadStream();
        string extension = Path.GetExtension(file.FileName).Trim();
        string fileLocation = $"folder/folder/{file.FileName}.{extension}";

        string uploadedPath = await _storageService.UploadStreamAsync(stream, fileLocation, "containerName");

        if (string.IsNullOrWhiteSpace(uploadedPath))
            return BadRequest("File upload error!");

        return Ok("File uploaded successfully!");
    }
}
```

### Upload File (Minimal API)

```csharp
public static class UploadEndpoint
{
    public static void MapUploadEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("v1/upload-file", UploadFile);
    }

    private static async Task<IResult> UploadFile(HttpContext httpContext, [FromServices] IStorageService storageService)
    {
        IFormCollection? form = await httpContext.Request.ReadFormAsync();
        IFormFile? file = form.Files.GetFile("file");

        if (file is null)
            return Results.BadRequest("No file provided!");

        using Stream stream = file.OpenReadStream();
        string extension = Path.GetExtension(file.FileName).Trim();
        string fileLocation = $"folder/folder/{file.FileName}.{extension}";

        string uploadedPath = await storageService.UploadStreamAsync(stream, fileLocation, "containerName");

        if (string.IsNullOrWhiteSpace(uploadedPath))
            return Results.BadRequest("File upload error!");

        return Results.Ok("File uploaded successfully!");
    }
}
```

### Delete File

Simply call:

```csharp
await _storageService.DeleteFileAsync("folder/folder/file.extension", "containerName");
```

## Contributing

If you would like to contribute, create a new branch and submit a pull request with your changes. Review may be needed before acceptance.

## Authors

@cenksari

## License

MIT

