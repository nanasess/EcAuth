using ConsoleAppFramework;
using IdentityProvider.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
var configuration = new ConfigurationBuilder()
    .AddEnvironmentVariables()
    .Build();
using var host = Host.CreateDefaultBuilder()
    .ConfigureServices((hostContext, services) => {
        services.AddDbContext<EcAuthDbContext>(options =>
            options.UseSqlServer(configuration["ConnectionStrings:EcAuthDbContext"]))
            .BuildServiceProvider();
    }).Build();
ConsoleApp.ServiceProvider = host.Services;
var app = ConsoleApp.Create();
    
app.Run(args);
