namespace Domain.Adapters;

public class OrderTalhaoProcessing
{
    private Guid Guid { get; set; }
    public uint Id { get; set; } // _ID
    public uint IdArea { get; set; } // id_area_emp
    public uint IdBloco { get; set; } // id_bloco
    public uint IdSituacao { get; set; } // id_situacao
    public DateTime DataSituacao { get; set; } // dt_situacao
    public uint IdMotivo { get; set; } // id_motivo
    public string? Motivo { get; set; } // ds_motivo_obs
    public uint IdUsuario { get; init; } // id_usuario_situacao
    public uint IdEquipe { get; set; } // id_equipe_situacao
    public required string ImeiColetor { get; set; } // cd_imei_situacao
    public string? Observacao { get; set; } // ds_obs
    public required string ProgramacaoGuid { get; set; } // id_programacao_guid
    public required string ProgramacaoRetornoGuid { get; set; } // id_programacao_retorno_guid
    public uint IdExportacao { get; set; } // id_exportacao
    public char? SnNovo { get; set; } // sn_novo
    public required string Latitude { get; set; } // vl_latitude
    public required string Longitude { get; set; } // vl_longitude
    public required string NomeArquivo { get; set; } // file
    public required string IdCiclo { get; set; } // ciclo
    public uint IdEmpresa { get; set; } // id_empresa
    public required string Modulo { get; set; } // modulo
    private DateTime CreateAt { get; set; }

    public OrderTalhaoProcessing()
    {
        Guid = Guid.NewGuid();
        CreateAt = DateTime.Now;
    }
    
}