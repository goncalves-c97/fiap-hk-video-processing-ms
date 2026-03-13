using Core.Constants;
using Core.Enums;
using Core.Events;
using Core.Exceptions;
using Core.Interfaces;
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
                    string zipUrl;

                    try
                    {
                        zipUrl = await _handler.HandleAsync(evt, _videoProcessingGateway, _storage, _frameExtractor);
                    }
                    catch (VideoNotFoundException ex)
                    {
                        Console.WriteLine($"Vídeo não encontrado para processamento: {ex.Message}");

                        await _consumer.PublishAsync("processing-error", new VideoProcessedEvent
                        {
                            VideoId = evt.VideoId,
                            UserId = evt.UserId,
                            UserEmail = evt.UserEmail,
                            OriginalVideoName = evt.OriginalVideoName,
                            ProcessedVideoUrl = string.Empty,
                            ProcessedAt = DateTime.UtcNow,
                            StatusVideoEnum = StatusVideoEnum.NotFound
                        });

                        return; // Não reprocessar, pois o vídeo não existe
                    }
                    catch (VideoProcessingException ex)
                    {
                        Console.WriteLine($"Erro ao processar vídeo {evt.VideoId}: {ex.Message}");

                        if (ex.Attempts == 1)
                        {
                            await _consumer.PublishAsync("processing-error", new VideoProcessedEvent
                            {
                                VideoId = evt.VideoId,
                                UserId = evt.UserId,
                                UserEmail = evt.UserEmail,
                                OriginalVideoName = evt.OriginalVideoName,
                                ProcessedVideoUrl = string.Empty,
                                ProcessedAt = DateTime.UtcNow,
                                StatusVideoEnum = StatusVideoEnum.Error
                            });
                        }
                        else if (ex.Attempts >= ConstantValues.MAX_ATTEMPTS)
                        {
                            Console.WriteLine($"Vídeo {evt.VideoId} atingiu o número máximo de tentativas. Marcando como erro permanente.");

                            await _consumer.PublishAsync("processing-error", new VideoProcessedEvent
                            {
                                VideoId = evt.VideoId,
                                UserId = evt.UserId,
                                UserEmail = evt.UserEmail,
                                OriginalVideoName = evt.OriginalVideoName,
                                ProcessedVideoUrl = string.Empty,
                                ProcessedAt = DateTime.UtcNow,
                                StatusVideoEnum = StatusVideoEnum.ErrorAttemptsExceeded
                            });

                            return;
                        }

                        throw;
                    }

                    await _consumer.PublishAsync("video-processed", new VideoProcessedEvent
                    {
                        VideoId = evt.VideoId,
                        UserId = evt.UserId,
                        UserEmail = evt.UserEmail,
                        OriginalVideoName = evt.OriginalVideoName,
                        ProcessedVideoUrl = zipUrl,
                        ProcessedAt = DateTime.UtcNow,
                        StatusVideoEnum = StatusVideoEnum.Completed
                    });
                });

            Console.WriteLine("VideoProcessingWorker iniciado e aguardando mensagens...");

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
