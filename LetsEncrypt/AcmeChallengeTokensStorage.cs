using System.Collections.Concurrent;

namespace LetsEncrypt
{
    internal static class AcmeChallengeTokensStorage
    {
        private static readonly ConcurrentDictionary<string, string> Tokens = new ConcurrentDictionary<string, string>();

        public static void AddToken(string token, string keyAuthz)
        {
            Tokens.AddOrUpdate(token, token, (existingToken, newToken) => newToken);
        }

        public static string GetTokenKey(string token)
        {
            Tokens.TryGetValue(token, out var key);
            return key;
        }
    }
}