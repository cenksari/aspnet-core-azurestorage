namespace AzureStorage
{
	using System;

	public record BlobFile
	{
		public string Type { get; init; }
		public string Name { get; init; }
		public long Length { get; init; }
		public string StorageUrl { get; init; }
		public DateTime? LastModified { get; init; }
	}
}