namespace Core.Interfaces
{
    public interface IMessagingService
    {
        Task PublishAsync<T>(string routingKey, T message);
        void Subscribe<T>(string queue, Func<T, Task> handler);
    }
}
