namespace Core.Interfaces
{
    public interface IFrameExtractionPathProvider
    {
        string GetTempVideoPath(Guid videoGuid);
        string GetFramesDirectory(Guid videoGuid);
        string GetZipPath(Guid videoGuid);
    }
}
