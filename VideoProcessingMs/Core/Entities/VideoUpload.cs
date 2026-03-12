using Core.Enums;
using Core.Helpers;

namespace Core.Entities
{
    public class VideoUpload : ValidatorClass
    {
        public int IdVideo { get; set; }

        public Guid Guid { get; set; } = Guid.NewGuid();

        public int IdUsuario { get; set; }

        public string NomeArquivoOriginal { get; set; }

        public string CaminhoStorageOriginal { get; set; }

        public string? CaminhoZipProcessado { get; set; }

        public long TamanhoBytes { get; set; }

        public string? TipoMime { get; set; }

        public StatusVideoEnum Status { get; set; }

        public DateTime DataHoraUpload { get; set; }

        public DateTime? DataHoraInicioProcessamento { get; set; }

        public DateTime? DataHoraFimProcessamento { get; set; }

        public string? MensagemErro { get; set; }

        public int TentativasProcessamento { get; set; }

        public VideoUpload() { }

        public VideoUpload(int idUsuario, string nomeArquivoOriginal, string caminhoStorageOriginal)
        {
            IdUsuario = idUsuario;
            NomeArquivoOriginal = nomeArquivoOriginal;
            CaminhoStorageOriginal = caminhoStorageOriginal;
            Status = StatusVideoEnum.Pending;
            ValidateValueObjects();
        }

        public void ValidateValueObjects()
        {
            Validate();
        }

        protected override void Validate()
        {
            IdValidation(IdUsuario, nameof(IdUsuario), validateZero: true);
            NotEmptyStringValidation(nameof(NomeArquivoOriginal), NomeArquivoOriginal);
            NotEmptyStringValidation(nameof(CaminhoStorageOriginal), CaminhoStorageOriginal);   
        }
    }
}
