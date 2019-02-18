using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace LetsEncrypt
{
    public static class LetsEncryptExtensions
    {
        public static ListenOptions UseLetsEncrypt(this ListenOptions options)
        {
            var httpsOptions = new HttpsConnectionAdapterOptions { ServerCertificateSelector = ServerCertificateSelector };
            return options.UseHttps(httpsOptions);
        }

        private static X509Certificate2 ServerCertificateSelector(ConnectionContext arg1, string arg2)
        {
            Console.Out.WriteLine("CERT REQUEST");
            var certificate = new X509Certificate2("cert.pfx", "asd123");
            return certificate;
        }
    }
}