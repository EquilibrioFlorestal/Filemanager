using Domain.Interfaces;
using Peixe.Database.Context;
using Peixe.Database.Services;
using Peixe.SMDP.Worker;
using Spectre.Console;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<Worker>();
builder.Services.AddDbContext<AppDbContext>(ServiceLifetime.Transient);
builder.Services.AddDbContext<EPFDbContext>(ServiceLifetime.Transient);

builder.Services.AddScoped<ITalhaoService, TalhaoService>();
builder.Services.AddScoped<IArquivoService, ArquivoService>();
builder.Services.AddScoped<IImagemService, ImagemService>();

builder.Logging.ClearProviders();

IHost host = builder.Build();

host.Run();

AnsiConsole.MarkupLine("\n[cyan]System[/]: Pressione [cyan]ENTER[/] para sair");
Console.ReadLine();