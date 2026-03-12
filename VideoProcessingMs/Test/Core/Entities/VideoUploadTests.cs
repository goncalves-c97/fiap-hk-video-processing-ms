using Core.Entities;
using Core.Helpers;

namespace Test.Core.Entities;

public class VideoProcessingTests
{
    [Fact]
    public void VideoProcessing_WhenValid_SetsPropertiesAndIsValid()
    {
        // Arrange
        int idUsuario = 1;
        string nomeArquivo = "nomeArquivo.mp4";
        string caminhoStorage = "caminho/storage/original";

        // Act
        var c = new VideoUpload(idUsuario, nomeArquivo, caminhoStorage);

        // Assert
        Assert.True(c.IsValid);
        Assert.Equal(idUsuario, c.IdUsuario);
        Assert.Equal(nomeArquivo, c.NomeArquivoOriginal);
        Assert.Equal(caminhoStorage, c.CaminhoStorageOriginal);
    }

    [Fact]
    public void VideoProcessing_WhenMissingFields_RegisterErrors()
    {
        // Arrange
        int idUsuario = 0;
        string nomeArquivo = null;
        string caminhoStorage = null;

        // Act
        var c = new VideoUpload(idUsuario, nomeArquivo, caminhoStorage);

        // Assert
        Assert.False(c.IsValid);
        Assert.True(c.ContainsError(GenericErrors.IdZeroError, nameof(VideoUpload.IdUsuario)));
        Assert.True(c.ContainsError(GenericErrors.EmptyStringError, nameof(VideoUpload.NomeArquivoOriginal)));
        Assert.True(c.ContainsError(GenericErrors.EmptyStringError, nameof(VideoUpload.CaminhoStorageOriginal)));
    }

    [Fact]
    public void VideoProcessing_WhenIdIsNegative_RegisterErrors()
    {
        // Arrange
        int idUsuario = -1;
        string nomeArquivo = null;
        string caminhoStorage = null;

        // Act
        var c = new VideoUpload(idUsuario, nomeArquivo, caminhoStorage);

        Assert.False(c.IsValid);
        Assert.True(c.ContainsError(GenericErrors.NegativeIdError, nameof(VideoUpload.IdUsuario)));
        Assert.True(c.ContainsError(GenericErrors.EmptyStringError, nameof(VideoUpload.NomeArquivoOriginal)));
        Assert.True(c.ContainsError(GenericErrors.EmptyStringError, nameof(VideoUpload.CaminhoStorageOriginal)));
    }
}
