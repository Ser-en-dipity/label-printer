using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using ICNC.ERP.Rpi;
using Grpc.Core;
using System;
using ICNC.Rpi;
using Grpc.Net.ClientFactory;

namespace QR.Api {
  public class Startup {
    public Startup(IConfiguration configuration) {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services
    // to the container.
    public void ConfigureServices(IServiceCollection services) {

      services.AddControllers();
      services.AddSwaggerGen(c => {
        c.SwaggerDoc("v1",
                     new OpenApiInfo { Title = "QR.Api", Version = "v1" });
      });

      services.AddOptions<MinioOptions>()
          .Bind(Configuration.GetSection(MinioOptions.Minio))
          .ValidateDataAnnotations();
      GrpcOption grpcOption =
              Configuration.GetSection(GrpcOption.GRPC).Get<GrpcOption>();
      services.AddGrpcClient<PrintCmdHandler.PrintCmdHandlerClient>(o =>
      {
          o.Address = new Uri(grpcOption.Endpoint);
      })
      .ConfigureChannel(copt => {
          if (!grpcOption.SSL) {
            copt.Credentials = ChannelCredentials.Insecure;
          }
        });
      services.AddHostedService<Worker>();
    }

    // This method gets called by the runtime. Use this method to configure the
    // HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
      if (env.IsDevelopment()) {
        app.UseDeveloperExceptionPage();
        app.UseSwagger();
        app.UseSwaggerUI(
            c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "QR.Api v1"));
      }

      // app.UseHttpsRedirection();

      app.UseRouting();

      app.UseAuthorization();

      app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
    }
  }
}
