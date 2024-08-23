using Domain.Interfaces;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Peixe.Database.Context;
using Peixe.Database.Services;
using Peixe.DICE.Worker;
using Serilog;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

Task task = WebHost.CreateDefaultBuilder(args)
    .ConfigureServices(services => services.AddHealthChecks())
    .Configure(app => app.UseRouting()
        .UseEndpoints(config => config
            .MapHealthChecks("/health", new HealthCheckOptions { Predicate = r => true })))
    .UseKestrel()
    .Build()
    .StartAsync();

IConfiguration configuration = builder.Configuration;

builder.Services.AddHostedService<Worker>();
builder.Services.AddDbContext<AppDbContext>(ServiceLifetime.Transient);
builder.Services.AddDbContext<EPFDbContext>(ServiceLifetime.Transient);

builder.Services.AddScoped<ITalhaoService, TalhaoService>();
builder.Services.AddScoped<IArquivoService, ArquivoService>();
builder.Services.AddScoped<IImagemService, ImagemService>();
builder.Services.AddScoped<IProgramacaoService, ProgramacaoService>();

builder.Services.AddWindowsService();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(AppDomain.CurrentDomain.GetAssemblies()));

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.File("./logs/log-.txt", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 5)
    .CreateLogger();

builder.Logging.AddSerilog();

IHost host = builder.Build();

host.Run();