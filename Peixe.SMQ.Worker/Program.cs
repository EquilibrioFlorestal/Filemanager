using Domain.Interfaces;
using Peixe.Database.Context;
using Peixe.Database.Services;
using Peixe.SMQ.Worker;
using Serilog;
using Spectre.Console;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .CreateLogger();

builder.Services.AddHostedService<Worker>();
builder.Services.AddDbContext<AppDbContext>(ServiceLifetime.Transient);
builder.Services.AddDbContext<EPFDbContext>(ServiceLifetime.Transient);

builder.Services.AddScoped<ITalhaoService, TalhaoService>();
builder.Services.AddScoped<IArquivoService, ArquivoService>();
builder.Services.AddScoped<IImagemService, ImagemService>();
builder.Services.AddScoped<IBlocoService, BlocoService>();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

IHost host = builder.Build();

host.Run();

AnsiConsole.MarkupLine("\n[cyan]System[/]: Pressione [cyan]ENTER[/] para sair");
Console.ReadLine();