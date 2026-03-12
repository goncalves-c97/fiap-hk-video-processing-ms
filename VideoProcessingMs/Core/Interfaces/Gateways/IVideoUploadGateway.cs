using Core.Entities;

namespace Core.Interfaces.Gateways
{
    public interface IVideoProcessingGateway
    {
        public Task<VideoUpload?> GetById(int idVideo, int idUsuario);
        public Task<VideoUpload?> GetByGuid(Guid guid, int idUsuario);
        public Task Update(VideoUpload videoUpload);
    }
}
