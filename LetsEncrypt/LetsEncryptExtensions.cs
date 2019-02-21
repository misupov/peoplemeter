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
            Console.Out.WriteLine("[LetsEncrypt] Logging in");
            var server = WellKnownServers.LetsEncryptV2;
            var (acme, account) = await GetAccount("lam0x86@gmail.com", server);
            Console.Out.WriteLine("[LetsEncrypt] Logged in");

            try
            {
                var orderCtx = await GetOrCreateOrder(acme, account, "peoplemeter.ru");

                var authz = (await orderCtx.Authorizations()).First();
                Console.Out.WriteLine("[LetsEncrypt] Authorizations passed");
                var httpChallenge = await authz.Http();
                Console.Out.WriteLine("[LetsEncrypt] http challenge ok");
                var token = httpChallenge.Token;
                var keyAuthz = httpChallenge.KeyAuthz;
                AcmeChallengeTokensStorage.AddToken(token, keyAuthz);
                Console.Out.WriteLine("[LetsEncrypt] Validate token");
                var challenge = await httpChallenge.Validate();
                while (challenge.Status != ChallengeStatus.Valid)
                {
                    Console.Out.WriteLine("[LetsEncrypt] Re-validate token");
                    await Task.Delay(5000);
                    challenge = await httpChallenge.Validate();
                }

                var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
                var privateKeyDer = privateKey.ToDer();
                Console.Out.WriteLine("[LetsEncrypt] Creating Csr");
                var certificationRequestBuilder = await orderCtx.CreateCsr(privateKey);

                Console.Out.WriteLine("[LetsEncrypt] Finalizing order");
                var order = await orderCtx.Finalize(certificationRequestBuilder.Generate());
                Console.Out.WriteLine("[LetsEncrypt] order.Status = " + order.Status);

                Console.Out.WriteLine("[LetsEncrypt] Export the certificate in PEM");
                var certificateChain = await orderCtx.Download();

                Console.Out.WriteLine("Saving the Certificate.");
                var certPfx = certificateChain.ToPfx(privateKey);
                var password = "abcd1234";
                _certificate = new X509Certificate2(certPfx.Build("cert", password), password);
                Console.Out.WriteLine("Certificate saved.");
                Console.Out.WriteLine("Certificate Verify: " + _certificate.Verify());
            }
            catch (Exception e)
            {

            }
        }

        private static async Task<IOrderContext> GetOrCreateOrder(AcmeContext acme, IAccountContext account, string domain)
        {
            var orderListContext = await account.Orders();
            var orderContexts = await orderListContext.Orders();
            var order = orderContexts.FirstOrDefault();
            if (order != null)
            {
                Console.Out.WriteLine("[LetsEncrypt] Using existed order");
                return order;
            }

            Console.Out.WriteLine("[LetsEncrypt] Creating new order");
            return await acme.NewOrder(new[] { domain });
        }

        private static async Task<(AcmeContext, IAccountContext)> GetAccount(string email, Uri server)
        {
            try
            {
                var pemKey = await File.ReadAllTextAsync($"{LetsEncryptAccountPem}-{email}", Encoding.ASCII);
                var accountKey = KeyFactory.FromPem(pemKey);
                var acme = new AcmeContext(server, accountKey);
                Console.Out.WriteLine("[LetsEncrypt] Account restored");
                return (acme, await acme.Account());
            }
            catch
            {
                Console.Out.WriteLine("[LetsEncrypt] Creating new account");
                var acme = new AcmeContext(server);
                var account = await acme.NewAccount(email, true);
                var pemKey = acme.AccountKey.ToPem();
                await File.WriteAllTextAsync($"{LetsEncryptAccountPem}-{email}", pemKey, Encoding.ASCII);
                Console.Out.WriteLine("[LetsEncrypt] Account created");
                return (acme, account);
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