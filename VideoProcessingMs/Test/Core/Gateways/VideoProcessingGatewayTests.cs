using Core.Entities;
using Core.Enums;
using Core.Gateways;
using Test.Helpers.Fakes;

namespace Test.Core.Gateways;

public class VideoProcessingGatewayTests
{
    [Fact]
    public async Task GetById_ShouldQueryExpectedTableAndParameters()
    {
        var expected = new VideoUpload();
        var dbConnection = new FakeDbConnection();
        dbConnection.SearchFirstOrDefaultHandler = (table, whereClause, whereParams) =>
        {
            Assert.Equal("VideoUpload", table);
            Assert.Equal("id_usuario = @Id AND id_video = @IdVideo", whereClause);
            Assert.Equal(7, ReadProperty<int>(whereParams, "Id"));
            Assert.Equal(13, ReadProperty<int>(whereParams, "IdVideo"));
            return expected;
        };

        var gateway = new VideoProcessingGateway(dbConnection);

        var result = await gateway.GetById(13, 7);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task GetByGuid_ShouldQueryExpectedTableAndParameters()
    {
        var guid = Guid.NewGuid();
        var expected = new VideoUpload();
        var dbConnection = new FakeDbConnection();
        dbConnection.SearchFirstOrDefaultHandler = (table, whereClause, whereParams) =>
        {
            Assert.Equal("VideoUpload", table);
            Assert.Equal("id_usuario = @Id AND guid = @Guid", whereClause);
            Assert.Equal(9, ReadProperty<int>(whereParams, "Id"));
            Assert.Equal(guid, ReadProperty<Guid>(whereParams, "Guid"));
            return expected;
        };

        var gateway = new VideoProcessingGateway(dbConnection);

        var result = await gateway.GetByGuid(guid, 9);

        Assert.Same(expected, result);
    }

    [Fact]
    public async Task Update_ShouldPersistAllMappedFields()
    {
        var dbConnection = new FakeDbConnection();
        var startedAt = DateTime.UtcNow.AddMinutes(-2);
        var finishedAt = DateTime.UtcNow;
        var video = new VideoUpload
        {
            IdVideo = 99,
            Guid = Guid.NewGuid(),
            IdUsuario = 5,
            NomeArquivoOriginal = "sample.mp4",
            CaminhoStorageOriginal = "uploads/sample.mp4",
            CaminhoZipProcessado = "processed/sample.zip",
            TamanhoBytes = 1234,
            TipoMime = "video/mp4",
            Status = StatusVideoEnum.Completed,
            DataHoraUpload = DateTime.UtcNow.AddMinutes(-5),
            DataHoraInicioProcessamento = startedAt,
            DataHoraFimProcessamento = finishedAt,
            MensagemErro = "none",
            TentativasProcessamento = 2
        };

        var gateway = new VideoProcessingGateway(dbConnection);

        await gateway.Update(video);

        var update = Assert.Single(dbConnection.Updates);
        Assert.Equal("VideoUpload", update.Table);
        Assert.Equal("id_video = @IdVideo AND id_usuario = @IdUsuario", update.WhereClause);
        Assert.Equal(99, ReadProperty<int>(update.WhereParams, "IdVideo"));
        Assert.Equal(5, ReadProperty<int>(update.WhereParams, "IdUsuario"));
        Assert.Equal(video.Guid, update.Values["guid"]);
        Assert.Equal(video.IdUsuario, update.Values["id_usuario"]);
        Assert.Equal(video.NomeArquivoOriginal, update.Values["nome_arquivo_original"]);
        Assert.Equal(video.CaminhoStorageOriginal, update.Values["caminho_storage_original"]);
        Assert.Equal(video.CaminhoZipProcessado, update.Values["caminho_zip_processado"]);
        Assert.Equal(video.TamanhoBytes, update.Values["tamanho_bytes"]);
        Assert.Equal(video.TipoMime, update.Values["tipo_mime"]);
        Assert.Equal(video.Status, update.Values["status"]);
        Assert.Equal(video.DataHoraUpload, update.Values["data_hora_upload"]);
        Assert.Equal(startedAt, update.Values["data_hora_inicio_processamento"]);
        Assert.Equal(finishedAt, update.Values["data_hora_fim_processamento"]);
        Assert.Equal(video.MensagemErro, update.Values["mensagem_erro"]);
        Assert.Equal(video.TentativasProcessamento, update.Values["tentativas_processamento"]);
    }

    private static T ReadProperty<T>(object? instance, string propertyName)
    {
        Assert.NotNull(instance);
        var property = instance!.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return Assert.IsType<T>(property!.GetValue(instance));
    }
}
