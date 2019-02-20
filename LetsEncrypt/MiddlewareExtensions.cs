using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace LetsEncrypt
{
    public static class MiddlewareExtensions
    {
        private const string WellKnownAcmeChallengePathPrefix = "/.well-known/acme-challenge/";

        public static IApplicationBuilder UseLetsEncrypt(this IApplicationBuilder app)
        {
            app.MapWhen(IsLetsEncryptRequest, HandleLetsEncryptRequest);

            return app;
        }

        private static void HandleLetsEncryptRequest(IApplicationBuilder app)
        {
            app.Run(async context =>
            {
                var path = context.Request.Path.Value;
                var token = path.Substring(WellKnownAcmeChallengePathPrefix.Length);
                var key = AcmeChallengeTokensStorage.GetTokenKey(token);
                Console.Out.WriteLine(context.Request.Path + " :: " + key);
                await context.Response.WriteAsync(key);
            });
        }

        private static bool IsLetsEncryptRequest(HttpContext context)
        {
            Console.Out.WriteLine(context.Request.Path);
            if (context.Request.IsHttps)
            {
                return false;
            }

            var path = context.Request.Path.Value;
            var isAcmeChallenge = path.StartsWith(WellKnownAcmeChallengePathPrefix);
            if (!isAcmeChallenge)
            {
                return false;
            }

            return true;
        }
    }
}