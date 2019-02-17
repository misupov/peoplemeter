using System;
using System.Text;
using System.Threading.Tasks;
using PikaFetcher.Parser;

namespace PikaFetcher
{
    internal static class Program
    {
        private static async Task Main()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var api = new PikabuApi();
            await api.Init();

            var fetchers = new[]
            {
                new RandomFetcher(api).FetchLoop(),
                new TopFetcher(api, 500, TimeSpan.FromDays(7)).FetchLoop()
            };

            await Task.WhenAll(fetchers);
        }
    }
}