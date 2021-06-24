using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

Host.CreateDefaultBuilder(args)
    .UseSerilog()
    .ConfigureWebHostDefaults(b => b.UseStartup<Swizzle.Startup>())
    .Build()
    .Run();
