using Core.Dtos;
using Core.Entities;
using Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Factories
{
    public static class VideoProcessingFactory
    {
        public static VideoUpload Create(VideoProcessingDto VideoProcessingDto)
        {
            return new VideoUpload
            {
                IdUsuario = VideoProcessingDto.IdUsuario,
                NomeArquivoOriginal = VideoProcessingDto.NomeArquivoOriginal,
                CaminhoStorageOriginal = VideoProcessingDto.CaminhoStorageOriginal,
                TamanhoBytes = VideoProcessingDto.TamanhoBytes,
                TipoMime = VideoProcessingDto.TipoMime,
                Status = StatusVideoEnum.Pending,
                DataHoraUpload = DateTime.UtcNow,
                TentativasProcessamento = 0
            };
        }
    }
}
