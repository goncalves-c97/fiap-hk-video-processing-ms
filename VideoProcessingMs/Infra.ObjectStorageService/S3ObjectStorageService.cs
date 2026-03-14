using Amazon.S3;
using Amazon.S3.Model;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Infra.ObjectStorageService
{
    public class S3ObjectStorageService : IObjectStorageService
    {
        private readonly IAmazonS3 _s3Client;
        private readonly string _bucket;
        private readonly string _serviceUrl = "https://s3.amazonaws.com";

        public S3ObjectStorageService(
            IAmazonS3 s3Client,
            IConfiguration configuration)
        {
            _s3Client = s3Client;
            _bucket = configuration["OBJ_STORAGE:BUCKET_NAME"];
            _serviceUrl = configuration["OBJ_STORAGE:SERVICE_URL"];
        }

        public async Task<string> UploadAsync(
            Stream fileStream,
            string fileName,
            string contentType)
        {
            var key = $"videos/{fileName}";

            var request = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = fileStream,
                ContentType = contentType
            };

            await _s3Client.PutObjectAsync(request);

            return key;
        }

        public async Task<Stream> DownloadAsync(string path)
        {
            var response = await _s3Client.GetObjectAsync(_bucket, path);
            return response.ResponseStream;
        }

        public async Task DeleteAsync(string path)
        {
            await _s3Client.DeleteObjectAsync(_bucket, path);
        }

        public string GetFileUrl(string path)
        {
            return $"{_serviceUrl}/{_bucket}/videos/{path}";
        }

        public string GetPresignedUrl(string path, int expirationMinutes = 60)
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucket,
                Key = path,
                Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
                Verb = HttpVerb.GET
            };

            return _s3Client.GetPreSignedURL(request);
        }
    }
}
