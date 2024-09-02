using Domain.Adapters;
using System.Globalization;

namespace Domain.Models;
public class Programacao
{
    public Int32 Id { get; set; }
    public Guid IdProgramacaoGuid { get; set; }

    public Int32 IdBloco { get; set; }
    public Int32 IdAreaEmp { get; set; }
    public Int32 IdTipoLevantamento { get; set; }

    public Int32 IdSituacao { get; set; }
    public DateTime? DataProgramacao { get; set; }
    public DateTime? DataSituacao { get; set; }
    public Int32? IdMotivoSituacao { get; set; }
    public String? ObservacaoUsuario { get; set; }

    public String? Equipe { get; set; }

    public Int32? IdUsuarioSituacao { get; set; }

    public Char SnNovo { get; set; }
    public Int32? IdExportacao { get; set; }
    public Int32? IdEquipeSituacao { get; set; }
    public String? ImeiSituacao { get; set; }

    public Guid? IdProgramacaoRetornoGuid { get; set; }

    public Decimal? Latitude { get; set; }
    public Decimal? Longitude { get; set; }


    public Programacao Atualizar(OrderTalhaoProcessing request)
    {
        this.IdSituacao = (Int32)request.IdSituacao;
        this.DataSituacao = request.DataSituacao;
        this.IdMotivoSituacao = (Int32)request.IdMotivo;
        this.ObservacaoUsuario = request.Observacao;
        this.IdUsuarioSituacao = (Int32)request.IdUsuario;
        this.SnNovo = request.SnNovo ?? 'N';
        this.ImeiSituacao = request.ImeiColetor;
        this.Latitude = Math.Round(Decimal.TryParse(request.Latitude, NumberStyles.Any, CultureInfo.InvariantCulture, out Decimal lat) ? lat : Decimal.Zero, 6);
        this.Longitude = Math.Round(Decimal.TryParse(request.Longitude, NumberStyles.Any, CultureInfo.InvariantCulture, out Decimal lng) ? lng : Decimal.Zero, 6);
        this.IdExportacao = (Int32)request.IdExportacao;
        this.IdEquipeSituacao = (Int32)request.IdEquipe;
        this.IdProgramacaoRetornoGuid = Guid.TryParse(request.ProgramacaoRetornoGuid, out Guid guid) ? guid : null;

        return this;
    }
}
