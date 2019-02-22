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

        private static X509Certificate2 _activeCertificate;

        public static ListenOptions UseLetsEncrypt(this ListenOptions options)
        {
            _ = LoadCertificate("peoplemeter.ru", "lam0x86@gmail.com");

            var httpsOptions = new HttpsConnectionAdapterOptions { ServerCertificateSelector = ServerCertificateSelector };
            return options.UseHttps(httpsOptions);
        }

        private static async Task LoadCertificate(string domain, string email)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            var staging = environment == EnvironmentName.Development || true; // TODO: GET RID OF THIS "TRUE"
            try
            {
                using (var store = new X509Store(StoreName.My, StoreLocation.CurrentUser))
                {
                    store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                    try
                    {
                        var certificate = store.Certificates.Find(X509FindType.FindBySubjectName, domain, false)
                            .Cast<X509Certificate2>()
                            .OrderByDescending(c => c.NotAfter)
                            .FirstOrDefault();

                        if (certificate != null)
                        {
                            Console.Out.WriteLine("[LetsEncrypt] Certificate found");
                            if (certificate.NotAfter < DateTime.Now.AddDays(10))
                            {
                                Console.Out.WriteLine($"[LetsEncrypt] But it's too old (expiration date is {certificate.NotAfter})");
                                Console.Out.WriteLine("[LetsEncrypt] Requesting new certificate");
                                await CreateCertificate(domain, email, staging);
                            }
                            else
                            {
                                Console.Out.WriteLine("[LetsEncrypt] Using certificate from local store");
                                _activeCertificate = certificate;
                            }
                        }
                        else
                        {
                            await CreateCertificate(domain, email, staging);
                        }
                    }
                    finally
                    {
                        store.Close();
                    }
                }
            }
            catch
            {
                await CreateCertificate(domain, email, staging);
            }
        }

        private static async Task CreateCertificate(string domain, string email, bool staging)
        {
            Console.Out.WriteLine("[LetsEncrypt] Logging in");
            var server = staging ? WellKnownServers.LetsEncryptStagingV2 : WellKnownServers.LetsEncryptV2;
            var (acme, account) = await GetAccount(email, server);
            Console.Out.WriteLine("[LetsEncrypt] Logged in");

            try
            {
                var orderCtx = await GetOrCreateOrder(acme, account, domain);

                var authz = (await orderCtx.Authorizations()).First();
                Console.Out.WriteLine("[LetsEncrypt] Authorizations passed");
                var httpChallenge = await authz.Http();
                Console.Out.WriteLine("[LetsEncrypt] http challenge ok");
                var token = httpChallenge.Token;
                var keyAuthz = httpChallenge.KeyAuthz;
                AcmeChallengeTokensStorage.AddToken(token, keyAuthz);
                Console.Out.WriteLine("[LetsEncrypt] Validating token");
                await Task.Delay(5000);
                var challenge = await httpChallenge.Validate();
                while (challenge.Status != ChallengeStatus.Valid)
                {
                    Console.Out.WriteLine("[LetsEncrypt] Re-validating token");
                    await Task.Delay(1000);
                    challenge = await httpChallenge.Validate();
                }

                Console.Out.WriteLine("[LetsEncrypt] Token valid");

                var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
                Console.Out.WriteLine("[LetsEncrypt] Creating Csr");
                var certificationRequestBuilder = await orderCtx.CreateCsr(privateKey);

                Console.Out.WriteLine("[LetsEncrypt] Finalizing order");
                var order = await orderCtx.Finalize(certificationRequestBuilder.Generate());
                Console.Out.WriteLine("[LetsEncrypt] order.Status = " + order.Status);

                Console.Out.WriteLine("[LetsEncrypt] Downloading Certificate Chain");
                var certificateChain = await orderCtx.Download();

                Console.Out.WriteLine("[LetsEncrypt] Saving the Certificate");
                var certPfx = certificateChain.ToPfx(privateKey);
                var password = "";
                var certData = certPfx.Build(domain, password);
                _activeCertificate = new X509Certificate2(certData, password);
                SaveCertificate(_activeCertificate, staging);
                Console.Out.WriteLine("[LetsEncrypt] Certificate saved");
            }
            catch (Exception e)
            {
                Console.Out.WriteLine(e);
            }
        }

        private static void SaveCertificate(X509Certificate2 certificate, bool staging)
        {
            var store = staging
                ? new X509Store("staging", StoreLocation.CurrentUser)
                : new X509Store(StoreName.My, StoreLocation.CurrentUser);

            using (store)
            {
                store.Open(OpenFlags.ReadWrite);
                try
                {
                    store.Add(certificate);
                }
                finally
                {
                    store.Close();
                }
            }
        }

        private static async Task<IOrderContext> GetOrCreateOrder(AcmeContext acme, IAccountContext account, string domain)
        {
            var orderListContext = await account.Orders();
            var orderContexts = await orderListContext.Orders();
            var order = orderContexts.FirstOrDefault();
            if (order != null)
            {
                Console.Out.WriteLine("[LetsEncrypt] Using existing order");
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
            Console.Out.WriteLine($"CERT REQUEST: {_activeCertificate == null}");
            Console.Out.WriteLine($"HasPrivateKey: {_activeCertificate.HasPrivateKey} {_activeCertificate.Verify()}");
            
            return _activeCertificate;
        }
    }
}