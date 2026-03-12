namespace Core.Interfaces
{
    public interface IFrameExtractor
    {
        public Task<Stream> ExtractFramesAsync(Stream videoStream, Guid videoGuid, int fps);
    }
}
