using Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Peixe.Database.Context;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Arquivo> Arquivos { get; set; }
    public DbSet<Imagem> Imagens { get; set; }
    public DbSet<Talhao> Talhoes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer(
            connectionString: "",
            options => options.CommandTimeout(180).EnableRetryOnFailure(5));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        OnModelCreatingArquivo(modelBuilder);
        OnModelCreatingImagem(modelBuilder);
        OnModelCreatingTalhao(modelBuilder);
    }

    private void OnModelCreatingImagem(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Imagem>().ToTable("Download_images_log");

        modelBuilder.Entity<Imagem>().HasKey(x => x.Id);

        modelBuilder.Entity<Imagem>().Property(x => x.Id).HasColumnName("_ID").ValueGeneratedOnAdd();
        modelBuilder.Entity<Imagem>().Property(x => x.CaminhoArquivoZip).HasColumnName("caminho_zip");
        modelBuilder.Entity<Imagem>().Property(x => x.CreateAt).HasColumnName("create_at").HasColumnType("datetime");
        modelBuilder.Entity<Imagem>().Property(x => x.ProgramacaoRetornoGuid).HasColumnName("id_programacao_retorno_guid");
        modelBuilder.Entity<Imagem>().Property(x => x.NomeImagem).HasColumnName("nome_imagem");
    }

    private void OnModelCreatingArquivo(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Arquivo>().ToTable("Download_files_log");

        modelBuilder.Entity<Arquivo>().HasKey(x => x.NomeArquivo);
        modelBuilder.Entity<Arquivo>().Property(x => x.Id).HasColumnName("_ID").ValueGeneratedOnAdd();

        modelBuilder.Entity<Arquivo>().Property(x => x.IdEmpresa).HasColumnName("id_empresa");
        modelBuilder.Entity<Arquivo>().Property(x => x.NomeUsuario).HasColumnName("name");
        modelBuilder.Entity<Arquivo>().Property(x => x.NomeMaquina).HasColumnName("hostname");
        modelBuilder.Entity<Arquivo>().Property(x => x.NomeArquivo).HasColumnName("file");
        modelBuilder.Entity<Arquivo>().Property(x => x.TamanhoBytes).HasColumnName("size");
        modelBuilder.Entity<Arquivo>().Property(x => x.QuantidadeImagens).HasColumnName("images");
        modelBuilder.Entity<Arquivo>().Property(x => x.Modulo).HasColumnName("modulo");
        modelBuilder.Entity<Arquivo>().Property(x => x.QuantidadeTalhoes).HasColumnName("qtd_talhoes");
        modelBuilder.Entity<Arquivo>().Property(x => x.CreateAt).HasColumnName("create_at");
    }

    private void OnModelCreatingTalhao(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Talhao>().ToTable("Download_talhoes_log");

        modelBuilder.Entity<Talhao>().HasKey(x => x.ProgramacaoRetornoGuid);
        modelBuilder.Entity<Talhao>().Property(x => x.Id).HasColumnName("_ID").ValueGeneratedOnAdd();

        modelBuilder.Entity<Talhao>().Property(x => x.IdArea).HasColumnName("id_area_emp");
        modelBuilder.Entity<Talhao>().Property(x => x.IdBloco).HasColumnName("id_bloco");
        modelBuilder.Entity<Talhao>().Property(x => x.IdSituacao).HasColumnName("id_situacao");
        modelBuilder.Entity<Talhao>().Property(x => x.DataSituacao).HasColumnName("dt_situacao").HasColumnType("datetime2");
        modelBuilder.Entity<Talhao>().Property(x => x.IdMotivo).HasColumnName("id_motivo");
        modelBuilder.Entity<Talhao>().Property(x => x.Motivo).HasColumnName("ds_motivo_obs");
        modelBuilder.Entity<Talhao>().Property(x => x.IdUsuario).HasColumnName("id_usuario_situacao");
        modelBuilder.Entity<Talhao>().Property(x => x.IdEquipe).HasColumnName("id_equipe_situacao");
        modelBuilder.Entity<Talhao>().Property(x => x.ImeiColetor).HasColumnName("cd_imei_situacao");
        modelBuilder.Entity<Talhao>().Property(x => x.Observacao).HasColumnName("ds_obs");
        modelBuilder.Entity<Talhao>().Property(x => x.ProgramacaoGuid).HasColumnName("id_programacao_guid");
        modelBuilder.Entity<Talhao>().Property(x => x.ProgramacaoRetornoGuid).HasColumnName("id_programacao_retorno_guid");
        modelBuilder.Entity<Talhao>().Property(x => x.IdExportacao).HasColumnName("id_exportacao");
        modelBuilder.Entity<Talhao>().Property(x => x.SnNovo).HasColumnName("sn_novo");
        modelBuilder.Entity<Talhao>().Property(x => x.Latitude).HasColumnName("vl_latitude");
        modelBuilder.Entity<Talhao>().Property(x => x.Longitude).HasColumnName("vl_longitude");
        modelBuilder.Entity<Talhao>().Property(x => x.NomeArquivo).HasColumnName("file");
        modelBuilder.Entity<Talhao>().Property(x => x.IdCiclo).HasColumnName("ciclo");
        modelBuilder.Entity<Talhao>().Property(x => x.IdEmpresa).HasColumnName("id_empresa");
        modelBuilder.Entity<Talhao>().Property(x => x.Modulo).HasColumnName("modulo");
        modelBuilder.Entity<Talhao>().Property(x => x.CreateAt).HasColumnName("dt_exportacao").HasColumnType("datetime2");
    }
}