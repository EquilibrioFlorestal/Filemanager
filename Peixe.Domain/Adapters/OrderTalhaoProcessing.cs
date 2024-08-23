namespace Domain.Adapters;

public class OrderTalhaoProcessing
{
    private Guid Guid { get; set; }
    public UInt32 Id { get; set; } // _ID
    public UInt32 IdArea { get; set; } // id_area_emp
    public UInt32 IdBloco { get; set; } // id_bloco
    public UInt32 IdSituacao { get; set; } // id_situacao
    public DateTime DataSituacao { get; set; } // dt_situacao
    public UInt32 IdMotivo { get; set; } // id_motivo
    public String? Motivo { get; set; } // ds_motivo_obs
    public UInt32 IdUsuario { get; init; } // id_usuario_situacao
    public UInt32 IdEquipe { get; set; } // id_equipe_situacao
    public required String ImeiColetor { get; set; } // cd_imei_situacao
    public String? Observacao { get; set; } // ds_obs
    public required String ProgramacaoGuid { get; set; } // id_programacao_guid
    public required String ProgramacaoRetornoGuid { get; set; } // id_programacao_retorno_guid
    public UInt32 IdExportacao { get; set; } // id_exportacao
    public Char? SnNovo { get; set; } // sn_novo
    public required String Latitude { get; set; } // vl_latitude
    public required String Longitude { get; set; } // vl_longitude
    public required String NomeArquivo { get; set; } // file
    public required String IdCiclo { get; set; } // ciclo
    public UInt32 IdEmpresa { get; set; } // id_empresa
    public required String Modulo { get; set; } // modulo
    private DateTime CreateAt { get; set; }

    public OrderTalhaoProcessing()
    {
        Guid = Guid.NewGuid();
        CreateAt = DateTime.Now;
    }

}