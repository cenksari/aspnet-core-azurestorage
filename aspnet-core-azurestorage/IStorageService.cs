namespace AzureStorage
{
	using System.Collections.Generic;
	using System.IO;
	using System.Threading.Tasks;

	public interface IStorageService
	{
		Task DeleteFileAsync(string cloudFilePath, string containerName);

		Task<BlobFile> GetInfoAsync(string cloudFilePath, string containerName);

		Task UploadStreamAsync(Stream stream, string cloudFilePath, string containerName);

		Task<List<BlobFile>> ListFilesAsync(string cloudDirectoryPath, string containerName, string delimiter = "/");
	}
}