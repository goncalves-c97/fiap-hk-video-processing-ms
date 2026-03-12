using Core.Interfaces;
using Core.Events;
using Core.Interfaces.Gateways;

namespace VideoProcessorWorker
{
    public class VideoProcessingWorker : BackgroundService
    {
        private readonly IMessagingService _consumer;
        private readonly IVideoProcessingHandler _handler;
        private readonly IVideoProcessingGateway _videoProcessingGateway;
        private readonly IObjectStorageService _storage;
        private readonly IFrameExtractor _frameExtractor;

        public VideoProcessingWorker(
            IMessagingService consumer,
            IVideoProcessingHandler handler,
            IVideoProcessingGateway videoProcessingGateway,
            IObjectStorageService objectStorageService,
            IFrameExtractor frameExtractor)
        {
            _consumer = consumer;
            _handler = handler;
            _videoProcessingGateway = videoProcessingGateway;
            _storage = objectStorageService;
            _frameExtractor = frameExtractor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("Iniciando VideoProcessingWorker...");

            _consumer.Subscribe<VideoUploadedEvent>(
                "video-uploaded",
                async evt =>
                {
                    await _handler.HandleAsync(evt, _videoProcessingGateway, _storage, _frameExtractor);
                });

            Console.WriteLine("VideoProcessingWorker iniciado e aguardando mensagens...");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
