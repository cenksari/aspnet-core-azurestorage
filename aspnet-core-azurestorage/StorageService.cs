namespace AzureStorage
{
	using Azure.Storage.Blobs;
	using Azure.Storage.Blobs.Models;
	using Microsoft.Extensions.Configuration;
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading.Tasks;

	/// <summary>
	/// Storage service.
	/// </summary>
	public class StorageService : IStorageService
	{
		private readonly BlobServiceClient BlobServiceClient;

		public StorageService(IConfiguration configuration) => BlobServiceClient = new(configuration["StorageConnectionString"]);

		/// <summary>
		/// Upload a file to blob storage.
		/// </summary>
		/// <param name="stream">File</param>
		/// <param name="cloudFilePath">Blob storage path</param>
		/// <param name="containerName">Container name</param>
		public async Task UploadStreamAsync(Stream stream, string cloudFilePath, string containerName)
		{
			BlobContainerClient blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);

			await blobContainerClient.CreateIfNotExistsAsync();

			BlobClient blobClient = blobContainerClient.GetBlobClient(cloudFilePath);

			stream.Position = 0;

			await blobClient.UploadAsync(stream);

			await UpdateContentTypeAsync(cloudFilePath, containerName);
		}

		/// <summary>
		/// Get file info from path.
		/// </summary>
		/// <param name="cloudFilePath">Blob storage path</param>
		/// <param name="containerName">Container name</param>
		public async Task<BlobFile> GetInfoAsync(string cloudFilePath, string containerName)
		{
			BlobContainerClient blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);

			await blobContainerClient.CreateIfNotExistsAsync();

			try
			{
				BlobClient blobClient = blobContainerClient.GetBlobClient(cloudFilePath);

				var properties = await blobClient.GetPropertiesAsync();

				return new BlobFile
				{
					Type = "blob",
					Name = Path.GetFileName(blobClient.Name),
					Length = properties.Value.ContentLength,
					StorageUrl = $"{blobContainerClient.Uri}/{blobClient.Name}",
					LastModified = properties.Value.LastModified.DateTime,
				};
			}
			catch (Exception)
			{
				return null;
			}
		}

		/// <summary>
		/// Delete a file from blob storage.
		/// </summary>
		/// <param name="cloudFilePath">Blob storage path</param>
		/// <param name="containerName">Container name</param>
		public async Task DeleteFileAsync(string cloudFilePath, string containerName)
		{
			BlobContainerClient blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);

			await blobContainerClient.CreateIfNotExistsAsync();

			BlobClient blobClient = blobContainerClient.GetBlobClient(cloudFilePath);

			await blobClient.DeleteIfExistsAsync();
		}

		/// <summary>
		/// List files from selected cloud folder.
		/// </summary>
		/// <param name="cloudDirectoryPath"></param>
		/// <param name="containerName">Container name</param>
		/// <param name="delimiter">Delimeter</param>
		public async Task<List<BlobFile>> ListFilesAsync(string cloudDirectoryPath, string containerName, string delimiter = "/")
		{
			BlobContainerClient blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);

			await blobContainerClient.CreateIfNotExistsAsync();

			List<BlobFile> directoryFiles = new();

			cloudDirectoryPath = FixDirectoryName(cloudDirectoryPath);

			string continuationToken = null;

			BlobItem blob;

			try
			{
				do
				{
					foreach (Azure.Page<BlobHierarchyItem> blobPage in blobContainerClient.GetBlobsByHierarchy(prefix: cloudDirectoryPath, delimiter: delimiter).AsPages(continuationToken))
					{
						foreach (BlobHierarchyItem blobHierarchyItem in blobPage.Values)
						{
							if (blobHierarchyItem.IsPrefix)
							{
								string name = blobHierarchyItem.Prefix;

								directoryFiles.Add
								(
									new BlobFile
									{
										Type = "directory",
										Name = new DirectoryInfo(name).Name,
										StorageUrl = $"{blobContainerClient.Uri}/{name}",
									}
								);
							}
							else
							{
								blob = blobHierarchyItem.Blob;

								directoryFiles.Add
								(
									new BlobFile
									{
										Type = "blob",
										Name = Path.GetFileName(blob.Name),
										Length = blob.Properties.ContentLength ?? 0,
										StorageUrl = $"{blobContainerClient.Uri}/{blob.Name}",
										LastModified = blob.Properties.LastModified?.DateTime,
									}
								);
							}
						}

						continuationToken = blobPage.ContinuationToken;
					}
				} while (continuationToken != "");

				return directoryFiles;
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
		}

		/// <summary>
		/// Fix directory name.
		/// </summary>
		/// <param name="path">Directory path</param>
		private static string FixDirectoryName(string path)
		{
			if (!string.IsNullOrEmpty(path))
			{
				string lastChar = path.Substring(path.Length - 1, 1);

				if (lastChar != "/")
					return path + "/";
				else
					return path;
			}

			return "/";
		}

		/// <summary>
		/// Update file mime type.
		/// </summary>
		/// <param name="cloudFilePath">Blob path</param>
		/// <param name="containerName">Container name</param>
		private async Task UpdateContentTypeAsync(string cloudFilePath, string containerName)
		{
			bool setProperties = false;

			string mimeType = "application/octet-stream";

			if (
				string.Equals(Path.GetExtension(cloudFilePath).ToLower(), ".jpg", StringComparison.OrdinalIgnoreCase)
				||
				string.Equals(Path.GetExtension(cloudFilePath).ToLower(), ".jpeg", StringComparison.OrdinalIgnoreCase)
				)
			{
				setProperties = true;
				mimeType = "image/jpeg";
			}
			else if (string.Equals(Path.GetExtension(cloudFilePath).ToLower(), ".mp4", StringComparison.OrdinalIgnoreCase))
			{
				setProperties = true;
				mimeType = "video/mp4";
			}
			else if (string.Equals(Path.GetExtension(cloudFilePath).ToLower(), ".mp3", StringComparison.OrdinalIgnoreCase))
			{
				setProperties = true;
				mimeType = "audio/mp3";
			}

			if (setProperties)
			{
				BlobContainerClient blobContainerClient = BlobServiceClient.GetBlobContainerClient(containerName);

				await blobContainerClient.CreateIfNotExistsAsync();

				BlobClient blobClient = blobContainerClient.GetBlobClient(cloudFilePath);

				BlobHttpHeaders blobHttpHeader = new()
				{
					ContentType = mimeType
				};

				await blobClient.SetHttpHeadersAsync(blobHttpHeader);
			}
		}
	}
}