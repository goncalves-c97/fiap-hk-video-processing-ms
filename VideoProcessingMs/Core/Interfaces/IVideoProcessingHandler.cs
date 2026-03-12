using Core.Events;
using Core.Interfaces.Gateways;

namespace Core.Interfaces
{
    public interface IVideoProcessingHandler
    {
        Task HandleAsync(VideoUploadedEvent videoEvent, IVideoProcessingGateway videoProcessingGateway, IObjectStorageService objectStorageService, IFrameExtractor frameExtractor, CancellationToken cancellationToken = default);
    }
}
