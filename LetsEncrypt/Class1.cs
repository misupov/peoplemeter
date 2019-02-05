using System;
using Certes;
using Certes.Acme;

namespace LetsEncrypt
{
    public class Class1
    {
        async void X()
        {
            var acme = new AcmeContext(WellKnownServers.LetsEncryptStagingV2);
            var account = await acme.NewAccount("admin@example.com", true);

            // Save the account key for later use
            var pemKey = acme.AccountKey.ToPem();
        }
    }
}
