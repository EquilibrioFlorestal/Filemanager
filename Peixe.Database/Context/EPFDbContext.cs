using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Peixe.Database.Context;

public class EPFDbContext : DbContext
{
    public EPFDbContext(DbContextOptions<EPFDbContext> options) : base(options) { }

    public DbSet<Bloco> Blocos { get; set; }
    public DbSet<Programacao> Programacoes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            connectionString: "",
            options => options.CommandTimeout(180).EnableRetryOnFailure(5));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreatingBloco(modelBuilder);
        OnModelCreatingProgramacao(modelBuilder);
    }

    protected void OnModelCreatingProgramacao(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Programacao>().ToTable("programacao");
        modelBuilder.Entity<Programacao>().HasKey(x => x.Id);

        modelBuilder.Entity<Programacao>().Property(x => x.Id).HasColumnName("_id");
        modelBuilder.Entity<Programacao>().Property(x => x.IdProgramacaoGuid).HasColumnName("id_programacao_guid");
        modelBuilder.Entity<Programacao>().Property(x => x.IdSituacao).HasColumnName("id_situacao");
        modelBuilder.Entity<Programacao>().Property(x => x.DataSituacao).HasColumnName("dt_situacao");
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
