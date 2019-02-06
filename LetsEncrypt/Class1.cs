using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Pkcs;

namespace LetsEncrypt
{
    public class Class1
    {
        public static async Task Main(string[] args)
        {
//            var acme = new AcmeContext(WellKnownServers.LetsEncryptV2);
//            var account = await acme.NewAccount("lam0x86@gmail.com", true);
//            
//            // Save the account key for later use
//            var pemKey = acme.AccountKey.ToPem();
//            File.WriteAllText(@"C:\temp\letsencrypt\pem", pemKey);

            var accountKey = KeyFactory.FromPem(File.ReadAllText(@"C:\temp\letsencrypt\pem"));
            var acme = new AcmeContext(WellKnownServers.LetsEncryptV2, accountKey);

            //var (uri, token, s) = await NewOrderHttp(acme, "peoplemeter.ru");
            await CheckOrderHttp(acme, "https://acme-v02.api.letsencrypt.org/acme/order/50945428/303515634");

            //            var newOrderLocation = newOrder.Location;
            //await CheckOrderDns(acme, "https://acme-v02.api.letsencrypt.org/acme/order/50945428/303477312");

//            if (validate.Status == ChallengeStatus.Valid)
//            {
//                var privateKey = KeyFactory.NewKey(KeyAlgorithm.ES256);
//                var cert = await order.Generate(new CsrInfo
//                {
//                    CountryName = "CA",
//                    State = "Ontario",
//                    Locality = "Toronto",
//                    Organization = "Certes",
//                    OrganizationUnit = "Dev",
//                    CommonName = "your.domain.name",
//                }, privateKey);
//            }
        }

        private static async Task<(Uri newOrderLocation, string token, string keyAuthz)> NewOrderHttp(AcmeContext acme, string domain)
        {
            var newOrder = await acme.NewOrder(new[] { domain });
            var newOrderLocation = newOrder.Location;
            var authz = (await newOrder.Authorizations()).First();
            var httpChallenge = await authz.Http();
            var keyAuthz = httpChallenge.KeyAuthz;
            var token = httpChallenge.Token;
            return (newOrderLocation, token, keyAuthz);
        }

        private static async Task<(Uri newOrderLocation, string dnsTxt)> NewOrderDns(AcmeContext acme, string domain)
        {
            var newOrder = await acme.NewOrder(new[] { domain });
            var newOrderLocation = newOrder.Location;
            var authz = (await newOrder.Authorizations()).First();
            var dnsChallenge = await authz.Dns();
            var dnsTxt = acme.AccountKey.DnsTxt(dnsChallenge.Token);
            return (newOrderLocation, dnsTxt);
        }

        private static async Task CheckOrderHttp(AcmeContext acme, string order)
        {
            var orderListContext = acme.Order(new Uri(order));
            var authorizationContexts = await orderListContext.Authorizations();

            var authz = authorizationContexts.First();
            var challenge = await authz.Http();
            //var validate = await challenge.Validate();

            var privateKey = KeyFactory.NewKey(KeyAlgorithm.RS256);
            
            var certChain = await orderListContext.Download();
            var build = certChain.ToPfx(privateKey).Build("cert", "abcd1234");
            File.WriteAllBytes(@"C:\temp\letsencrypt\cert.pfx", build);

            var cert = File.ReadAllBytes(@"C:\temp\letsencrypt\cert.pfx");
            var serverCertificate = new X509Certificate2(cert, "abcd1234");
            var serverCertificateHasPrivateKey = serverCertificate.HasPrivateKey;
        }

        private static async Task CheckOrderDns(AcmeContext acme, string order)
        {
            var orderListContext = acme.Order(new Uri(order));
            var authorizationContexts = await orderListContext.Authorizations();

            var authz = authorizationContexts.First();
            var dnsChallenge = await authz.Dns();
            var dnsTxt = acme.AccountKey.DnsTxt(dnsChallenge.Token);
            var validate = await dnsChallenge.Validate();
        }
    }
}
