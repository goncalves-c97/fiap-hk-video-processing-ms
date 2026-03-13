using Core.Enums;
using System.Data;

namespace Core.Events
{
    public class VideoProcessedEvent
    {
        public Guid VideoId { get; init; }
        public int UserId { get; init; }
        public string UserEmail { get; init; }
        public string OriginalVideoName { get; init; }
        public string ProcessedVideoUrl { get; init; }
        public StatusVideoEnum StatusVideoEnum { get; init; }
        public DateTime ProcessedAt { get; init; }
    }
}
