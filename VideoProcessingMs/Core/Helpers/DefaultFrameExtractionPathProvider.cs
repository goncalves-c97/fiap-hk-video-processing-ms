using Core.Interfaces;

namespace Core.Helpers
{
    public class DefaultFrameExtractionPathProvider : IFrameExtractionPathProvider
    {
        public string GetTempVideoPath(Guid videoGuid)
        {
            return Path.Combine(Path.GetTempPath(), $"{videoGuid}.mp4");
        }

        public string GetFramesDirectory(Guid videoGuid)
        {
            return Path.Combine(Path.GetTempPath(), videoGuid.ToString());
        }

        public string GetZipPath(Guid videoGuid)
        {
            return Path.Combine(Path.GetTempPath(), $"{videoGuid}.zip");
        }
    }
}
