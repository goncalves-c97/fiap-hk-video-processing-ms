using Core.Entities;
using Core.Interfaces;
using Core.Interfaces.Gateways;

namespace Core.Gateways
{
    public class VideoProcessingGateway(IDbConnection dbConnection) : IVideoProcessingGateway
    {
        private readonly IDbConnection _dbConnection = dbConnection;
        private const string _tableName = nameof(VideoUpload);

        public async Task<VideoUpload?> GetById(int idVideo, int idUsuario)
        {
            return await _dbConnection.SearchFirstOrDefaultByParametersAsync<VideoUpload>(
                _tableName,
                "id_usuario = @Id AND id_video = @IdVideo",
                new { Id = idUsuario, IdVideo = idVideo }
            );
        }

        public async Task<VideoUpload?> GetByGuid(Guid guid, int idUsuario)
        {
            return await _dbConnection.SearchFirstOrDefaultByParametersAsync<VideoUpload>(
                _tableName,
                "id_usuario = @Id AND guid = @Guid",
                new { Id = idUsuario, Guid = guid }
            );
        }

        public async Task Update(VideoUpload videoUpload)
        {
            await _dbConnection.UpdateAsync(_tableName, new Dictionary<string, object>
            {
                { "guid", videoUpload.Guid },
                { "id_usuario", videoUpload.IdUsuario },
                { "nome_arquivo_original", videoUpload.NomeArquivoOriginal },
                { "caminho_storage_original", videoUpload.CaminhoStorageOriginal },
                { "caminho_zip_processado", videoUpload.CaminhoZipProcessado },
                { "tamanho_bytes", videoUpload.TamanhoBytes },
                { "tipo_mime", videoUpload.TipoMime },
                { "status", videoUpload.Status },
                { "data_hora_upload", videoUpload.DataHoraUpload },
                { "data_hora_inicio_processamento", videoUpload.DataHoraInicioProcessamento },
                { "data_hora_fim_processamento", videoUpload.DataHoraFimProcessamento },
                { "mensagem_erro", videoUpload.MensagemErro },
                { "tentativas_processamento", videoUpload.TentativasProcessamento }
            }, "id_video = @IdVideo AND id_usuario = @IdUsuario", new { IdVideo = videoUpload.IdVideo, IdUsuario = videoUpload.IdUsuario });
        }
    }
}