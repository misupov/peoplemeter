using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace PikaWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var now = DateTimeOffset.Now;
            Console.WriteLine("Application started at: " + now);
            return WebHost.CreateDefaultBuilder(args)
                .UseKestrel(options =>
                {
                    options.Listen(IPAddress.Any, 443, listenOptions =>
                    {
                        var httpsOptions = new HttpsConnectionAdapterOptions {ServerCertificateSelector = ServerCertificateSelector};
                        listenOptions.UseHttps(httpsOptions);
                    });
                    options.Listen(IPAddress.Any, 80);
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>();
        }

        private static X509Certificate2 ServerCertificateSelector(ConnectionContext arg1, string arg2)
        {
            Console.Out.WriteLine("CERT REQUEST");
            var certificate = new X509Certificate2("cert.pfx", "asd123");
            return certificate;
        }
    }
}
