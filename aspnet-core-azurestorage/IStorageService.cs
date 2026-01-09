namespace AzureStorage
{
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    public interface IStorageService
    {
        Task DeleteFileAsync(string cloudFilePath, string containerName, CancellationToken cancellationToken = default);

        Task<byte[]> DownloadStreamAsync(string cloudFilePath, string containerName, CancellationToken cancellationToken = default);

        Task<string> UploadStreamAsync(Stream stream, string cloudFilePath, string containerName, CancellationToken cancellationToken = default);

        Task<List<BlobFile>> ListFilesAsync(string cloudDirectoryPath, string containerName, string delimiter = "/", CancellationToken cancellationToken = default);
    }
}