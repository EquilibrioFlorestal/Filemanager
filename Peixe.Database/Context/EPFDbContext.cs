using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Peixe.Database.Context;

public class EPFDbContext : DbContext
{
    public EPFDbContext(DbContextOptions<EPFDbContext> options) : base(options) { }

    public EPFDbContext() : base() { }

    public virtual DbSet<Bloco> Blocos { get; set; }
    public virtual DbSet<Programacao> Programacoes { get; set; }
    public virtual DbSet<Cadastro> Cadastros { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            connectionString: "",
            options => options.CommandTimeout(300).EnableRetryOnFailure(5));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreatingCadastro(modelBuilder);
        OnModelCreatingBloco(modelBuilder);
        OnModelCreatingProgramacao(modelBuilder);
    }

    protected void OnModelCreatingCadastro(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cadastro>().ToTable("area_emp");
        modelBuilder.Entity<Cadastro>().HasKey(x => x.Id);

        modelBuilder.Entity<Cadastro>().Property(x => x.Id).HasColumnName("_id");
        modelBuilder.Entity<Cadastro>().Property(x => x.Equipe).HasColumnName("no_equipe");
    }

    protected void OnModelCreatingProgramacao(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Programacao>().ToTable("programacao");
        modelBuilder.Entity<Programacao>().HasKey(x => x.Id);

        modelBuilder.Entity<Programacao>().Property(x => x.Id).HasColumnName("_id");
        modelBuilder.Entity<Programacao>().Property(x => x.IdProgramacaoGuid).HasColumnName("id_programacao_guid");
        modelBuilder.Entity<Programacao>().Property(x => x.IdAreaEmp).HasColumnName("id_area_emp");
        modelBuilder.Entity<Programacao>().Property(x => x.IdBloco).HasColumnName("id_bloco");
        modelBuilder.Entity<Programacao>().Property(x => x.Equipe).HasColumnName("no_equipe");
        modelBuilder.Entity<Programacao>().Property(x => x.IdTipoLevantamento).HasColumnName("id_tipo_levantamento");
        modelBuilder.Entity<Programacao>().Property(x => x.IdSituacao).HasColumnName("id_situacao");
        modelBuilder.Entity<Programacao>().Property(x => x.DataSituacao).HasColumnName("dt_situacao");
        modelBuilder.Entity<Programacao>().Property(x => x.DataProgramacao).HasColumnName("dt_monitor_prog");
        modelBuilder.Entity<Programacao>().Property(x => x.IdMotivoSituacao).HasColumnName("id_motivo");
        modelBuilder.Entity<Programacao>().Property(x => x.ObservacaoUsuario).HasColumnName("ds_motivo_obs");
        modelBuilder.Entity<Programacao>().Property(x => x.IdUsuarioSituacao).HasColumnName("id_usuario_situacao");
        modelBuilder.Entity<Programacao>().Property(x => x.SnNovo).HasColumnName("sn_novo");
        modelBuilder.Entity<Programacao>().Property(x => x.IdExportacao).HasColumnName("id_exportacao");
        modelBuilder.Entity<Programacao>().Property(x => x.IdEquipeSituacao).HasColumnName("id_equipe_situacao");
        modelBuilder.Entity<Programacao>().Property(x => x.ImeiSituacao).HasColumnName("cd_imei_situacao");
        modelBuilder.Entity<Programacao>().Property(x => x.IdProgramacaoRetornoGuid).HasColumnName("id_programacao_retorno_guid");
        modelBuilder.Entity<Programacao>().Property(x => x.Latitude).HasColumnName("vl_latitude").HasColumnType("numeric").HasPrecision(12);
        modelBuilder.Entity<Programacao>().Property(x => x.Longitude).HasColumnName("vl_longitude").HasColumnType("numeric").HasPrecision(12);

    }

    protected void OnModelCreatingBloco(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Bloco>().ToTable("blocos");

        modelBuilder.Entity<Bloco>().HasKey(x => x.Id);
        modelBuilder.Entity<Bloco>().Property(x => x.Id).HasColumnName("_id");
        modelBuilder.Entity<Bloco>().Property(x => x.IdCiclo).HasColumnName("id_ciclo");
        modelBuilder.Entity<Bloco>().Property(x => x.Descricao).HasColumnName("no_Bloco");
    }

}
