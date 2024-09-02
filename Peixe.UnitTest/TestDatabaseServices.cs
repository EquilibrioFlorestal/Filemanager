using Domain.Adapters;
using Domain.Constants;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.EntityFrameworkCore;
using Peixe.Database.Context;
using Peixe.Database.Services;

namespace Peixe.UnitTest;

[TestClass]
public class TestImagemDatabaseServices
{
    private Mock<DbSet<Imagem>> _mockSet;
    private Mock<AppDbContext> _mockContext;
    private Mock<IServiceScope> _serviceScopeMock;
    private Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private ImagemService _imagemService;

    [TestInitialize]
    public void Setup()
    {

        Imagem imagem1 = new Imagem { CaminhoArquivoZip = String.Empty, NomeImagem = "Imagem1.jpg", ProgramacaoRetornoGuid = "c50abde4-a651-4395-9aee-805302b5fc08", Id = 1 };
        Imagem imagem2 = new Imagem { CaminhoArquivoZip = String.Empty, NomeImagem = "Imagem2.jpg", ProgramacaoRetornoGuid = "ec719138-dd9f-49a8-abe9-d70a7a64e006", Id = 2 };

        List<Imagem> imagens = new List<Imagem>
        {
            imagem1,
            imagem2
        };

        _mockSet = new Mock<DbSet<Imagem>>();

        _mockSet.As<IQueryable<Imagem>>().Setup(m => m.Provider).Returns(imagens.AsQueryable().Provider);
        _mockSet.As<IQueryable<Imagem>>().Setup(m => m.Expression).Returns(imagens.AsQueryable().Expression);
        _mockSet.As<IQueryable<Imagem>>().Setup(m => m.ElementType).Returns(imagens.AsQueryable().ElementType);
        _mockSet.As<IQueryable<Imagem>>().Setup(m => m.GetEnumerator()).Returns(imagens.GetEnumerator());

        _mockContext = new Mock<AppDbContext>();
        _mockContext.Setup(x => x.Imagens).Returns(_mockSet.Object);

        Mock<IServiceProvider> scopedServiceProviderMock = new Mock<IServiceProvider>();
        scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(AppDbContext))).Returns(_mockContext.Object);

        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeMock.Setup(s => s.ServiceProvider).Returns(scopedServiceProviderMock.Object);

        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceScopeFactoryMock.Setup(f => f.CreateScope()).Returns(_serviceScopeMock.Object);

        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(_serviceScopeFactoryMock.Object);

        _imagemService = new ImagemService(_serviceProviderMock.Object);
    }

    [TestMethod]
    public async Task VerificarCadastrado_DeveriaRetornarVerdadeiro()
    {
        Boolean result = await _imagemService.VerificarCadastrado("Imagem1.jpg", "c50abde4-a651-4395-9aee-805302b5fc08");
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task CadastrarImagem_DeveriaCadastrarNovaImagem()
    {
        OrderImageProcessing orderImage = new("c50abde4-a651-4395-9aee-805302b5fc01", "Imagem5.jpg", String.Empty);
        (Boolean success, String message) = await _imagemService.CadastrarImagem(orderImage);

        Assert.IsTrue(success);
        Assert.AreEqual(String.Empty, message);

        _mockSet.Verify(m => m.Add(It.IsAny<Imagem>()), Times.Once());
        _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }
}

[TestClass]
public class TestBlocoDatabaseServices
{
    private readonly Mock<IServiceProvider> _serviceProvider;
    private Mock<EPFDbContext> _mockContext;
    private BlocoService _blocoService;
    private Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private Mock<IServiceScope> _serviceScopeMock;
    private Mock<IServiceProvider> _serviceProviderMock;

    [TestInitialize]
    public void Setup()
    {
        IQueryable<Bloco> blocos = new List<Bloco>
        {
            new Bloco{Descricao = "Bloco 1", Id = 1, IdCiclo = 102},
            new Bloco{Descricao = "Bloco 2", Id = 2, IdCiclo = 102},
            new Bloco{Descricao = "Bloco 3", Id = 3, IdCiclo = 103},
            new Bloco{Descricao = "Bloco 4", Id = 4, IdCiclo = 103},
        }.AsQueryable();

        _mockContext = new Mock<EPFDbContext>();

        _mockContext.Setup(x => x.Blocos).ReturnsDbSet(blocos);

        Mock<IServiceProvider> scopedServiceProviderMock = new Mock<IServiceProvider>();
        scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(EPFDbContext))).Returns(_mockContext.Object);

        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeMock.Setup(s => s.ServiceProvider).Returns(scopedServiceProviderMock.Object);

        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceScopeFactoryMock.Setup(f => f.CreateScope()).Returns(_serviceScopeMock.Object);

        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(_serviceScopeFactoryMock.Object);

        _blocoService = new BlocoService(_serviceProviderMock.Object);
    }

    [TestMethod]
    public async Task ListarBloco_DeveriaRetornarADescricaoDoBloco()
    {
        String resultado = await _blocoService.ListarBloco(1, 102);

        Assert.IsNotNull(resultado);
        Assert.AreNotEqual(String.Empty, resultado);
        Assert.AreEqual("Bloco 1", resultado);
    }

    [TestMethod]
    public async Task ListarBloco_DeveriaRetornarStringEmBranco()
    {
        String resultado = await _blocoService.ListarBloco(1, 103);

        Assert.IsNotNull(resultado);
        Assert.AreEqual(String.Empty, resultado);
    }

}

[TestClass]
public class TestArquivoDatabaseServices
{
    private readonly Mock<IServiceProvider> _serviceProvider;
    private Mock<AppDbContext> _mockContext;
    private ArquivoService _arquivoService;

    private Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private Mock<IServiceScope> _serviceScopeMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<DbSet<Arquivo>> _mockSet;

    [TestInitialize]
    public void Setup()
    {
        List<Arquivo> arquivos = new List<Arquivo>
        {
            new Arquivo() {Modulo= Modulo.DICE, NomeArquivo = "Arquivo1.zip", NomeMaquina = "EPF075", NomeUsuario = "gabriel.ac", IdEmpresa = 1},
            new Arquivo() {Modulo= Modulo.SOF, NomeArquivo = "Arquivo2.zip", NomeMaquina = "EPF075", NomeUsuario = "gabriel.ac", IdEmpresa = 1},
            new Arquivo() {Modulo= Modulo.SMPD, NomeArquivo = "Arquivo3.zip", NomeMaquina = "EPF075", NomeUsuario = "gabriel.ac", IdEmpresa = 1},
        };

        _mockSet = new Mock<DbSet<Arquivo>>();

        _mockSet.As<IQueryable<Arquivo>>().Setup(m => m.Provider).Returns(arquivos.AsQueryable().Provider);
        _mockSet.As<IQueryable<Arquivo>>().Setup(m => m.Expression).Returns(arquivos.AsQueryable().Expression);
        _mockSet.As<IQueryable<Arquivo>>().Setup(m => m.ElementType).Returns(arquivos.AsQueryable().ElementType);
        _mockSet.As<IQueryable<Arquivo>>().Setup(m => m.GetEnumerator()).Returns(arquivos.GetEnumerator());

        _mockContext = new Mock<AppDbContext>();
        _mockContext.Setup(x => x.Arquivos).Returns(_mockSet.Object);

        Mock<IServiceProvider> scopedServiceProviderMock = new Mock<IServiceProvider>();
        scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(AppDbContext))).Returns(_mockContext.Object);

        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeMock.Setup(s => s.ServiceProvider).Returns(scopedServiceProviderMock.Object);

        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceScopeFactoryMock.Setup(f => f.CreateScope()).Returns(_serviceScopeMock.Object);

        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(_serviceScopeFactoryMock.Object);

        _arquivoService = new ArquivoService(_serviceProviderMock.Object);

    }

    [TestMethod]
    public async Task VerificaCadastrado_DeveriaRetornarVerdadeiro()
    {
        Boolean arquivos = await _arquivoService.VerificarCadastrado("Arquivo1.zip", Modulo.DICE, 1);
        Assert.IsTrue(arquivos);
    }

    [TestMethod]
    public async Task CadastrarArquivo_DeveriaCadastrarNovoArquivo()
    {
        OrderProcessing order = new OrderProcessing(1, String.Empty, String.Empty, [String.Empty], Modulo.DICE)
        {
            NomeMaquina = String.Empty,
            NomeUsuario = String.Empty,
        };
        OrderFileProcessing orderFile = new OrderFileProcessing
        {
            Nome = "Arquivo1.zip",
            QuantidadeImagens = 1,
            QuantidadeTalhoes = 2,
            TamanhoBytes = 200,
        };

        (Boolean success, String message) = await _arquivoService.CadastrarArquivo(order, orderFile);

        Assert.IsTrue(success);
        Assert.AreEqual(String.Empty, message);

        _mockSet.Verify(m => m.Add(It.IsAny<Arquivo>()), Times.Once());
        _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

}

[TestClass]
public class TestProgramacaoDatabaseServices
{
    private readonly Mock<IServiceProvider> _serviceProvider;
    private Mock<EPFDbContext> _mockContext;
    private ProgramacaoService _programacaoService;

    private Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
    private Mock<IServiceScope> _serviceScopeMock;
    private Mock<IServiceProvider> _serviceProviderMock;
    private Mock<DbSet<Programacao>> _mockSet;

    [TestInitialize]
    public void Setup()
    {
        List<Programacao> programacoes = new()
        {
            new Programacao{
                Id = 1,
                IdProgramacaoGuid = Guid.Parse("c50abde4-a651-4395-9aee-805302b5fc08"),
                IdBloco = 1,
                IdAreaEmp = 1,
                IdTipoLevantamento = 101,
                IdSituacao = 1,
                DataProgramacao = new DateTime(2024, 08, 02),
                DataSituacao = null,
                IdMotivoSituacao = null,
                ObservacaoUsuario = null,
                Equipe = String.Empty,
                IdUsuarioSituacao = null,
                SnNovo = 'N',
                IdExportacao = null,
                IdEquipeSituacao = null,
                ImeiSituacao = String.Empty,
                IdProgramacaoRetornoGuid = Guid.Parse("c50abde4-a651-4395-9aee-805302b5fc08"),
                Latitude = null,
                Longitude= null,
            },
        };

        _mockSet = new Mock<DbSet<Programacao>>();

        _mockSet.As<IQueryable<Programacao>>().Setup(m => m.Provider).Returns(programacoes.AsQueryable().Provider);
        _mockSet.As<IQueryable<Programacao>>().Setup(m => m.Expression).Returns(programacoes.AsQueryable().Expression);
        _mockSet.As<IQueryable<Programacao>>().Setup(m => m.ElementType).Returns(programacoes.AsQueryable().ElementType);
        _mockSet.As<IQueryable<Programacao>>().Setup(m => m.GetEnumerator()).Returns(programacoes.GetEnumerator());

        _mockContext = new Mock<EPFDbContext>();
        _mockContext.Setup(x => x.Programacoes).ReturnsDbSet(_mockSet.Object);


        Mock<IServiceProvider> scopedServiceProviderMock = new();
        scopedServiceProviderMock.Setup(sp => sp.GetService(typeof(EPFDbContext))).Returns(_mockContext.Object);

        _serviceScopeMock = new Mock<IServiceScope>();
        _serviceScopeMock.Setup(s => s.ServiceProvider).Returns(scopedServiceProviderMock.Object);

        _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
        _serviceScopeFactoryMock.Setup(f => f.CreateScope()).Returns(_serviceScopeMock.Object);

        _serviceProviderMock = new Mock<IServiceProvider>();
        _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(_serviceScopeFactoryMock.Object);

        _programacaoService = new(_serviceProviderMock.Object);
    }

    [TestMethod]
    public async Task Atualizar_DeveriaRetornarProgramacaoExtraCadastrada()
    {

        List<Cadastro> cadastros = new()
        {
            new Cadastro
            {
                Id = 1,
                Equipe  = "Equipe 1"
            }
        };

        Mock<DbSet<Cadastro>> mockSet = new Mock<DbSet<Cadastro>>();

        mockSet.As<IQueryable<Cadastro>>().Setup(m => m.Provider).Returns(cadastros.AsQueryable().Provider);
        mockSet.As<IQueryable<Cadastro>>().Setup(m => m.Expression).Returns(cadastros.AsQueryable().Expression);
        mockSet.As<IQueryable<Cadastro>>().Setup(m => m.ElementType).Returns(cadastros.AsQueryable().ElementType);
        mockSet.As<IQueryable<Cadastro>>().Setup(m => m.GetEnumerator()).Returns(cadastros.GetEnumerator());

        _mockContext.Setup(x => x.Cadastros).ReturnsDbSet(mockSet.Object);
        _mockContext.Setup(m => m.Programacoes.Add(It.IsAny<Programacao>())).Verifiable();

        OrderTalhaoProcessing order = new()
        {
            IdCiclo = "1",
            ImeiColetor = "YYYYY",
            Latitude = "-10.0000",
            Longitude = "-10.0000",
            Modulo = Modulo.DICE,
            NomeArquivo = "Arquivo.zip",
            SnNovo = 'S',
            ProgramacaoGuid = "c50abde4-a651-4395-9aee-805302b5fc01",
            ProgramacaoRetornoGuid = "c50abde4-a651-4395-9aee-805302b5fc01",
            DataSituacao = DateTime.Now,
        };

        (Programacao? programacao, String mensagem) = await _programacaoService.Atualizar(order);

        Assert.IsNotNull(programacao);
        Assert.AreEqual("Sucesso.", mensagem);

        _mockContext.Verify(m => m.Programacoes.Add(It.IsAny<Programacao>()), Times.Once());
        _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }

    [TestMethod]
    public async Task Atualizar_DeveriaRetornarNullQuandoProgramacaoNaoExistir()
    {
        OrderTalhaoProcessing order = new()
        {
            IdCiclo = "1",
            ImeiColetor = "YYYYY",
            Latitude = "-10.0000",
            Longitude = "-10.0000",
            Modulo = Modulo.DICE,
            NomeArquivo = "Arquivo.zip",
            SnNovo = 'N',
            ProgramacaoGuid = "c50abde4-a651-4395-9aee-805302b5fc01",
            ProgramacaoRetornoGuid = "c50abde4-a651-4395-9aee-805302b5fc01"
        };

        (Programacao? programacao, String mensagem) = await _programacaoService.Atualizar(order);

        Assert.IsNull(programacao);
        Assert.AreEqual(String.Empty, mensagem);
    }

    [TestMethod]
    public async Task Atualizar_DeveriaAtualizarProgramacao()
    {
        OrderTalhaoProcessing order = new()
        {
            IdCiclo = "1",
            ImeiColetor = "YYYYY",
            Latitude = "-10.0000",
            Longitude = "-10.0000",
            Modulo = Modulo.DICE,
            NomeArquivo = "Arquivo.zip",
            ProgramacaoGuid = "c50abde4-a651-4395-9aee-805302b5fc08",
            ProgramacaoRetornoGuid = "c50abde4-a651-4395-9aee-805302b5fc08"
        };

        (Programacao? programacao, String mensagem) = await _programacaoService.Atualizar(order);

        Assert.IsNotNull(programacao);
        Assert.AreEqual("Sucesso.", mensagem);

        Assert.AreEqual((Int32)order.IdSituacao, programacao.IdSituacao);

        _mockContext.Verify(m => m.Update(It.IsAny<Programacao>()), Times.Once());
        _mockContext.Verify(m => m.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once());
    }
}