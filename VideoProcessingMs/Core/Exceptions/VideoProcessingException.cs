namespace Core.Exceptions
{
    public class VideoProcessingException : Exception
    {
        public int Attempts { get; set; }

        public VideoProcessingException(string message, int attempts) : base(message)
        {
            Attempts = attempts;
        }
    }
}
