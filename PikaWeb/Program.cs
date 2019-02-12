using System;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

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
            Console.WriteLine("!!!");
            var cert = File.ReadAllBytes("cert.pfx");
            X509Certificate2Collection c = new X509Certificate2Collection();

            var serverCertificate = new X509Certificate2(cert, "asd123", X509KeyStorageFlags.MachineKeySet);
            Console.WriteLine(serverCertificate.HasPrivateKey);
            return WebHost.CreateDefaultBuilder(args)
                .UseKestrel(options =>
                    {
                        options.Listen(IPAddress.Any, 443, listenOptions => { listenOptions.UseHttps(serverCertificate); });
                        options.Listen(IPAddress.Any, 80);
                    })
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseStartup<Startup>();
        }
    }
}
