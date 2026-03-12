using Amazon.S3;
using Amazon.S3.Model;
using Infra.ObjectStorageService;
using Microsoft.Extensions.Configuration;
using Moq;

namespace Test.Infra.ObjectStorageService;

public class S3ObjectStorageServiceTests
{
    [Fact]
    public async Task UploadAsync_BuildsExpectedPutRequest_AndReturnsKey()
    {
        var s3 = new Mock<IAmazonS3>(MockBehavior.Strict);
        var captured = new List<PutObjectRequest>();

        s3.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), default))
        .Callback<PutObjectRequest, CancellationToken>((r, _) => captured.Add(r))
        .ReturnsAsync(new PutObjectResponse());

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
 {
 { "OBJ_STORAGE:BUCKET_NAME", "my-bucket" }
 }).Build();

        var svc = new S3ObjectStorageService(s3.Object, config);
        using var ms = new MemoryStream(new byte[] { 1, 2, 3 });

        var key = await svc.UploadAsync(ms, "file.mp4", "video/mp4");

        Assert.StartsWith("videos/", key);
        Assert.EndsWith("-file.mp4", key);
        Assert.Single(captured);
        Assert.Equal("my-bucket", captured[0].BucketName);
        Assert.Equal(key, captured[0].Key);
        Assert.Same(ms, captured[0].InputStream);
        Assert.Equal("video/mp4", captured[0].ContentType);
    }

    [Fact]
    public async Task DownloadAsync_CallsGetObjectWithBucketAndPath_AndReturnsResponseStream()
    {
        var s3 = new Mock<IAmazonS3>(MockBehavior.Strict);
        var stream = new MemoryStream(new byte[] { 9, 8, 7 });

        s3.Setup(x => x.GetObjectAsync("my-bucket", "path/file", default))
        .ReturnsAsync(new GetObjectResponse { ResponseStream = stream });

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
 {
 { "OBJ_STORAGE:BUCKET_NAME", "my-bucket" }
 }).Build();

        var svc = new S3ObjectStorageService(s3.Object, config);

        var result = await svc.DownloadAsync("path/file");

        Assert.Same(stream, result);
    }

    [Fact]
    public async Task DeleteAsync_CallsDeleteObjectWithBucketAndPath()
    {
        var s3 = new Mock<IAmazonS3>(MockBehavior.Strict);

        s3.Setup(x => x.DeleteObjectAsync("my-bucket", "path/file", default))
        .ReturnsAsync(new DeleteObjectResponse());

        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
 {
 { "OBJ_STORAGE:BUCKET_NAME", "my-bucket" }
 }).Build();

        var svc = new S3ObjectStorageService(s3.Object, config);

        await svc.DeleteAsync("path/file");

        s3.VerifyAll();
    }
}
