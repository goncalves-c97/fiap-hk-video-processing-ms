namespace Core.Events
{
    public class VideoUploadedEvent
    {
        public Guid VideoId { get; init; }
        public int UserId { get; init; }
        public string UserEmail { get; init; }
        public string OriginalVideoName { get; init; }
        public string StoragePath { get; init; }
        public DateTime UploadedAt { get; init; }
    }
}
