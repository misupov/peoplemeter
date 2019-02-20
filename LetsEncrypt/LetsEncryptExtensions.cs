using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
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
        private static X509Certificate2 _certificate;

        public static ListenOptions UseLetsEncrypt(this ListenOptions options)
        {
            RenewCertificate();

            var httpsOptions = new HttpsConnectionAdapterOptions { ServerCertificateSelector = ServerCertificateSelector };
            return options.UseHttps(httpsOptions);
        }

        private static async void RenewCertificate()
        {
            var acme = new AcmeContext(WellKnownServers.LetsEncryptStagingV2);
            var account = await acme.NewAccount("lam0x86@gmail.com", true);

            var order = await acme.NewOrder(new[] {"peoplemeter.ru"});

            var authz = (await order.Authorizations()).First();
            var httpChallenge = await authz.Http();
            var token = httpChallenge.Token;
            var keyAuthz = httpChallenge.KeyAuthz;
            AcmeChallengeTokensStorage.AddToken(token, keyAuthz);

            var challenge = await httpChallenge.Validate();
            while (challenge.Status != ChallengeStatus.Valid)
            {
                challenge = await httpChallenge.Validate();
            }

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
        }

        private static X509Certificate2 ServerCertificateSelector(ConnectionContext arg1, string arg2)
        {
            Console.Out.WriteLine("CERT REQUEST");
            return _certificate;
        }
    }
}