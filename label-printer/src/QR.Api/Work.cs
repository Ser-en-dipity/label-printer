using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Google.Protobuf.WellKnownTypes;
using QR.Api;
using ICNC.Rpi;
using Minio;
using System.Net.Http;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Grpc.Net.Client;
using Microsoft.Extensions.Options;
using System;

namespace ICNC.ERP.Rpi{
    public class Worker : BackgroundService  {
        private readonly ILogger<Worker> _logger;
        private readonly MinioOptions _minioOptions;
        public IConfiguration Configuration { get; }

        private MinioClient Client =>
            new MinioClient(_minioOptions.EndPoint, _minioOptions.AccessKey,
                            _minioOptions.SecretKey)
                .WithSSL();
        public Worker(ILogger<Worker> logger,
                   IConfiguration configuration,
                   IOptions<MinioOptions> minioOptions){
            _logger = logger;
            Configuration = configuration;
            _minioOptions = minioOptions.Value;
        }
        protected  async override Task ExecuteAsync (CancellationToken cancellationToken){
            try{
                await DoWork();
                await Task.Delay(TimeSpan.FromMinutes(2));
            }
            catch (Grpc.Core.RpcException ex){
                _logger.LogWarning(default, ex, "grpc call failed");
            }
            catch (System.IO.IOException ex){
                _logger.LogWarning(default, ex , "grpc server is not running");
            }
        }
       
        public async Task DoWork(){
            GrpcOption grpcOption =
                Configuration.GetSection(GrpcOption.GRPC).Get<GrpcOption>();
            HttpClientHandler clientHandler = new HttpClientHandler();
            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };
            using var channel = GrpcChannel.ForAddress(grpcOption.Endpoint, new GrpcChannelOptions
            {
                HttpHandler = clientHandler,
                Credentials = new SslCredentials()
            });
            var client = new PrintCmdHandler.PrintCmdHandlerClient(channel);
            
            using var streamingCall = client.PrintLabel(new Empty());
                await foreach (var cmd in streamingCall.ResponseStream.ReadAllAsync()){
                    _logger.LogInformation("********************" + cmd.Bucket + cmd.FileName );
                    if (!await Client.BucketExistsAsync(cmd.Bucket)) {
                        _logger.LogInformation("no bucket !");
                        return ;
                    }
                    string tmpFileName = $"/tmp/{cmd.FileName}";

                    using var fs =
                        new FileStream(tmpFileName, FileMode.Create, FileAccess.ReadWrite);
                    await Client.GetObjectAsync(cmd.Bucket, cmd.FileName,
                                                (s) => { s.CopyTo(fs); });
                    fs.Close();
                    using var process = new System.Diagnostics.Process() {
                        StartInfo =
                            new System.Diagnostics.ProcessStartInfo() {
                            FileName = "lp", Arguments = $"-n {cmd.Times} {tmpFileName}",
                            UseShellExecute = false, RedirectStandardError = true,
                            RedirectStandardInput = true, RedirectStandardOutput = true
                            }
                    };
                    process.Start();
                    process.WaitForExit(4200);
                    while (!process.StandardOutput.EndOfStream)
                        _logger.LogInformation(process.StandardOutput.ReadLine());
                    while (!process.StandardError.EndOfStream)
                        _logger.LogError(process.StandardError.ReadLine());
                }
            
            await channel.ShutdownAsync();
            return ;
            
        }
        public Task StopAsync(CancellationToken cancellationToken){
            return Task.CompletedTask;
        }
    }
    
}
