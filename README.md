# Azure Storage helpers
Azure Storage helpers for your dotnet core project.

### How to install

1. Add `IStorageService.cs` and `StorageService.cs` to your project.
2. Install nuget package `Azure.Storage.Blobs` package.
3. Configure your configuration file: Add `StorageConnectionString` key and value.
4. Configure your `Startup.cs` file : Add `services.AddSingleton<IStorageService, StorageService>();`

### How to use

**List files:**

```csharp
private readonly IStorageService StorageService;

public HomeController(IStorageService StorageService) => StorageService = storageService;

public async Task<IActionResult> ListFiles()
{
  List<BlobFile> results = await StorageService.ListFilesAsync("folder/folder", "containerName");
  
  if (results != null)
  {
    ViewBag.Results = results;
  }

  return View();
}
```

**Upload file:**

```csharp
private readonly IStorageService StorageService;

public HomeController(IStorageService StorageService) => StorageService = storageService;

[HttpPost]
public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
{
  using Stream stream = file.OpenReadStream();
  
  string extension = Path.GetExtension(file.FileName).Trim();
  
  string fileLocation = $"folder/folder/{file.FileName}.{extension}";

  await StorageService.UploadStreamAsync(stream, fileLocation, "containerName");

  return View();
}
```

**Delete file:**

Simply call

```csharp
await Cache.DeleteFileAsync("folder/folder/file.extension", "containerName");
```
