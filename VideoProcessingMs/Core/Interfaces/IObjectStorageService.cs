namespace Core.Interfaces
{
    public interface IObjectStorageService
    {
        Task<string> UploadAsync(
            Stream fileStream,
            string fileName,
            string contentType);

        Task<Stream> DownloadAsync(string path);

        Task DeleteAsync(string path);
    }
}
