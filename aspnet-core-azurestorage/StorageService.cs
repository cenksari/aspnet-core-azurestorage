namespace AzureStorage;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Service responsible for managing file operations in Azure Blob Storage, including upload, download, and deletion of files.
/// </summary>
/// <param name="blobServiceClient">Injected Azure BlobServiceClient for storage operations</param>
public class StorageService(BlobServiceClient blobServiceClient) : IStorageService
{
    /// <summary>
    /// Blob service client for interacting with Azure Blob Storage.
    /// </summary>
    private readonly BlobServiceClient _blobServiceClient =
        blobServiceClient ?? throw new ArgumentNullException(nameof(blobServiceClient));

    /// <summary>
    /// Gets blob client.
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="cloudFilePath">Blob storage path</param>
    private BlobClient GetBlobClient(
        string containerName,
        string cloudFilePath) =>
        _blobServiceClient.GetBlobContainerClient(containerName).GetBlobClient(cloudFilePath);

    /// <summary>
    /// Known mime types for uploaded blobs.
    /// </summary>
    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".png", "image/png" },
            { ".mp4", "video/mp4" },
            { ".txt", "text/plain" },
            { ".mp3", "audio/mpeg" },
            { ".jpg", "image/jpeg" },
            { ".jpeg", "image/jpeg" },
            { ".pdf", "application/pdf" }
        };

    /// <summary>
    /// Downloads a file from blob storage.
    /// </summary>
    /// <param name="cloudFilePath">Blob storage path</param>
    /// <param name="containerName">Container name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<byte[]> DownloadStreamAsync(
        string cloudFilePath,
        string containerName,
        CancellationToken cancellationToken = default)
    {
        // Get blob client.
        BlobClient blobClient = GetBlobClient(containerName, cloudFilePath);

        // Create memory stream.
        await using MemoryStream stream = new();

        // Download the blob to the stream.
        await blobClient.DownloadToAsync(stream, cancellationToken);

        // Return the stream as byte array.
        return stream.ToArray();
    }

    /// <summary>
    /// Uploads a file to blob storage.
    /// </summary>
    /// <param name="stream">File</param>
    /// <param name="cloudFilePath">Blob storage path</param>
    /// <param name="containerName">Container name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<string> UploadStreamAsync(
        Stream stream,
        string cloudFilePath,
        string containerName,
        CancellationToken cancellationToken = default)
    {
        // Get container client.
        BlobContainerClient container = await GetContainerAsync(containerName, cancellationToken);

        // Get blob client.
        BlobClient blobClient = container.GetBlobClient(cloudFilePath);

        // Reset stream position.
        if (stream.CanSeek)
            stream.Position = 0;

        // Upload the stream to the blob.
        await blobClient.UploadAsync(stream, true, cancellationToken);

        // Update file mime type.
        await UpdateContentTypeAsync(blobClient, cloudFilePath, cancellationToken);

        // Return the blob uri.
        return blobClient.Uri.ToString();
    }

    /// <summary>
    /// Deletes a file from blob storage.
    /// </summary>
    /// <param name="cloudFilePath">Blob storage path</param>
    /// <param name="containerName">Container name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task DeleteFileAsync(
        string cloudFilePath,
        string containerName,
        CancellationToken cancellationToken = default)
    {
        // Get blob client.
        BlobClient blobClient = GetBlobClient(containerName, cloudFilePath);

        // Delete the blob if it exists.
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Lists files from selected cloud folder.
    /// </summary>
    /// <param name="cloudDirectoryPath"></param>
    /// <param name="containerName">Container name</param>
    /// <param name="delimiter">Delimeter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task<List<BlobFile>> ListFilesAsync(
        string cloudDirectoryPath,
        string containerName,
        string delimiter = "/",
        CancellationToken cancellationToken = default)
    {
        // Get container client.
        BlobContainerClient container = await GetContainerAsync(containerName, cancellationToken);

        // Normalize directory path.
        string prefix = NormalizeDirectoryPath(cloudDirectoryPath);

        // Prepare results list.
        List<BlobFile> results = [];

        // List blobs by hierarchy.
        await foreach (var item in container.GetBlobsByHierarchyAsync(
            prefix: prefix,
            delimiter: delimiter,
            traits: BlobTraits.None,
            states: BlobStates.None,
            cancellationToken: cancellationToken))
        {
            if (item.IsPrefix)
            {
                results.Add(new()
                {
                    Type = "directory",
                    Name = new DirectoryInfo(item.Prefix).Name,
                    StorageUrl = $"{container.Uri}/{item.Prefix}"
                });

                continue;
            }

            BlobItem? blob = item.Blob;

            results.Add(new()
            {
                Type = "blob",
                Name = Path.GetFileName(blob.Name),
                Length = blob.Properties.ContentLength ?? 0,
                LastModified = blob.Properties.LastModified?.DateTime,
                StorageUrl = $"{container.Uri}/{blob.Name}"
            });
        }

        return results;
    }

    /// <summary>
    /// Gets container client. Creates the container if it does not exist.
    /// </summary>
    /// <param name="containerName">Container name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task<BlobContainerClient> GetContainerAsync(
        string containerName,
        CancellationToken cancellationToken)
    {
        BlobContainerClient? container = _blobServiceClient.GetBlobContainerClient(containerName);

        await container.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        return container;
    }

    /// <summary>
    /// Fix directory name.
    /// </summary>
    /// <param name="path">Directory path</param>
    private static string NormalizeDirectoryPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;

        path = path.Replace("\\", "/");

        return path.EndsWith('/') ? path : path + "/";
    }

    /// <summary>
    /// Updates file mime type. Defaults to application/octet-stream if unknown.
    /// </summary>
    /// <param name="blobClient">Blob client</param>
    /// <param name="blobPath">Blob path</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private static async Task UpdateContentTypeAsync(
        BlobClient blobClient,
        string blobPath,
        CancellationToken cancellationToken)
    {
        // Get extension.
        string? extension = Path.GetExtension(blobPath);

        // If extension is unknown.
        if (!MimeTypes.TryGetValue(extension, out var contentType))
            contentType = "application/octet-stream";

        // Create blob headers.
        BlobHttpHeaders? blobHeaders = new() { ContentType = contentType };

        // Update the blob's HTTP headers.
        await blobClient.SetHttpHeadersAsync(blobHeaders, cancellationToken: cancellationToken);
    }
}