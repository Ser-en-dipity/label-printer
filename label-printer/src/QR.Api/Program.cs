using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using ICNC.Rpi;

namespace QR.Api {
  public class Program {
    private static string Env =>
        Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
    private static IConfiguration Configuration =>
        new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.{Env}.json", true)
            .Build();

    public static void Main(string[] args) {
      Log.Logger = new LoggerConfiguration()
                       .ReadFrom.Configuration(Configuration)
                       .CreateLogger();
      CreateHostBuilder(args).Build().Run();
      Log.CloseAndFlush();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((ctx, builder) => {
              builder.AddJsonFile("appsettings.json", false, true);
              builder.AddJsonFile($"appsettings.{Env}.json", true, false);
            })
            .ConfigureWebHostDefaults(webBuilder => {
              webBuilder.UseStartup<Startup>();
            })
            .UseSerilog();
  }
}
