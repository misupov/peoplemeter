using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Https;

namespace LetsEncrypt
{
    public static class LetsEncryptExtensions
    {
        private static readonly string LetsEncryptAccountPem = "lets-encrypt-account.pem";

        private static X509Certificate2 _certificate;

        public static ListenOptions UseLetsEncrypt(this ListenOptions options)
        {
            _ = RenewCertificate();

            var httpsOptions = new HttpsConnectionAdapterOptions { ServerCertificateSelector = ServerCertificateSelector };
            return options.UseHttps(httpsOptions);
        }

        private static async Task RenewCertificate()
        {
            await Task.Delay(5000);
            var acme = new AcmeContext(WellKnownServers.LetsEncryptV2);
            var account = GetAccount("lam0x86@gmail.com");

            var order = await acme.NewOrder(new[] {"peoplemeter.ru"});

            var authz = (await order.Authorizations()).First();
            var httpChallenge = await authz.Http();
            var token = httpChallenge.Token;
            var keyAuthz = httpChallenge.KeyAuthz;
            AcmeChallengeTokensStorage.AddToken(token, keyAuthz);
            Console.Out.WriteLine("Validate token");
            await Task.Delay(10000);
            var challenge = await httpChallenge.Validate();
            while (challenge.Status != ChallengeStatus.Valid)
            {
                Console.Out.WriteLine("Re-validate token");
                await Task.Delay(10000);
                challenge = await httpChallenge.Validate();
            }

            Console.Out.WriteLine("Saving the Certificate.");
            var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
            var cert = await order.Generate(new CsrInfo
            {
                CountryName = "CA",
                State = "Ontario",
                Locality = "Toronto",
                Organization = "Certes",
                OrganizationUnit = "Dev",
                CommonName = "peoplemeter.ru",
            }, privateKey);

            var pfxBuilder = cert.ToPfx(privateKey);
            var password = "abcd1234";
            _certificate = new X509Certificate2(pfxBuilder.Build("cert", password), password);
            Console.Out.WriteLine("Certificate saved.");
        }

        private static async Task<IAccountContext> GetAccount(string email)
        {
            try
            {
                var pemKey = await File.ReadAllTextAsync(LetsEncryptAccountPem, Encoding.ASCII);
                var accountKey = KeyFactory.FromPem(pemKey);
                var acme = new AcmeContext(WellKnownServers.LetsEncryptV2, accountKey);
                return await acme.Account();
            }
            catch
            {
                var acme = new AcmeContext(WellKnownServers.LetsEncryptV2);
                var account = await acme.NewAccount(email, true);
                var pemKey = acme.AccountKey.ToPem();
                await File.WriteAllTextAsync(LetsEncryptAccountPem, pemKey, Encoding.ASCII);
                return account;
            }
        }

        private static X509Certificate2 ServerCertificateSelector(ConnectionContext arg1, string arg2)
        {
            Console.Out.WriteLine($"CERT REQUEST: {_certificate == null}");
            Console.Out.WriteLine($"HasPrivateKey: {_certificate.HasPrivateKey} {_certificate.Verify()}");
            
            return _certificate;
        }
    }
}