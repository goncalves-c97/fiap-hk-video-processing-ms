using Core.Interfaces;

namespace Test.Helpers.Fakes;

public sealed class FakeObjectStorageService : IObjectStorageService
{
 public List<(string FileName, string ContentType)> Uploads { get; } = new();
 public List<string> Deletes { get; } = new();

 public Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
 {
 Uploads.Add((fileName, contentType));
 return Task.FromResult(fileName);
 }

 public Task<Stream> DownloadAsync(string path)
 {
 return Task.FromResult<Stream>(new MemoryStream());
 }

 public Task DeleteAsync(string path)
 {
 Deletes.Add(path);
 return Task.CompletedTask;
 }
}
