using Core.Constants;
using Core.Entities;
using Core.Enums;
using Core.Events;
using Core.Exceptions;
using Core.Interfaces;
using Core.Interfaces.Gateways;

namespace Core.Handlers
{
    public class VideoProcessingHandler : IVideoProcessingHandler
    {
        public async Task<string> HandleAsync(VideoUploadedEvent evt, IVideoProcessingGateway videoProcessingGateway, IObjectStorageService objectStorageService, IFrameExtractor frameExtractor, CancellationToken cancellationToken = default)
        {
            VideoUpload? video = null;

            try
            {
                Console.WriteLine($"Iniciando processamento do vídeo {evt.VideoId} para o usuário {evt.UserId}.");

                video = await videoProcessingGateway.GetByGuid(evt.VideoId, evt.UserId);

                if (video == null)
                {
                    Console.WriteLine($"Vídeo {evt.VideoId} não encontrado para o usuário {evt.UserId}.");
                    throw new VideoNotFoundException($"Vídeo {evt.VideoId} não encontrado.");
                }

                Console.WriteLine($"Vídeo {evt.VideoId} encontrado. Iniciando processamento...");

                // Atualiza status para Processing
                video.Status = StatusVideoEnum.Processing;
                video.DataHoraInicioProcessamento = DateTime.UtcNow;

                await videoProcessingGateway.Update(video);

                Console.WriteLine("Status atualizado para Processing. Baixando vídeo do storage...");

                // Baixa o vídeo do storage
                using var videoStream =
                    await objectStorageService.DownloadAsync(evt.StoragePath);

                Console.WriteLine("Vídeo baixado. Extraindo frames...");

                // Extrai frames
                using var zipStream =
                    await frameExtractor.ExtractFramesAsync(videoStream, video.Guid, 2); // Two frames per second

                Console.WriteLine("Frames extraídos. Criando ZIP e fazendo upload...");

                // Define caminho do ZIP
                var zipPath = $"{video.Guid}-processed/{video.Guid}.zip";

                // Upload do ZIP
                string fullZipPath = await objectStorageService.UploadAsync(
                    zipStream,
                    zipPath,
                    "application/zip");

                string zipUrl = objectStorageService.GetPresignedUrl(fullZipPath);

                Console.WriteLine("ZIP criado e enviado para o storage. Atualizando status para Completed...");

                // Atualiza status para Completed
                video.Status = StatusVideoEnum.Completed;
                video.CaminhoZipProcessado = zipUrl;
                video.DataHoraFimProcessamento = DateTime.UtcNow;

                await videoProcessingGateway.Update(video);

                Console.WriteLine($"Processamento do vídeo {evt.VideoId} concluído com sucesso.");

                return zipUrl;
            }
            catch (VideoNotFoundException)
            {
                Console.WriteLine($"Vídeo {evt.VideoId} não encontrado. Encerrando processamento.");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar o vídeo {evt.VideoId}: {ex.Message}");

                if (video != null)
                {
                    video.Status = StatusVideoEnum.Error;
                    video.MensagemErro = ex.Message;
                    video.TentativasProcessamento++;

                    if (video.TentativasProcessamento >= ConstantValues.MAX_ATTEMPTS)
                        video.Status = StatusVideoEnum.ErrorAttemptsExceeded;

                    await videoProcessingGateway.Update(video);

                    Console.WriteLine($"Status atualizado para Error. Tentativas de processamento: {video.TentativasProcessamento}");

                    throw new VideoProcessingException(ex.Message, video.TentativasProcessamento);
                }
                else
                    throw new VideoProcessingException(ex.Message, -1);
            }
        }
    }
}
