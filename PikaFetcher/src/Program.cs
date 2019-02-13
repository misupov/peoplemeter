using System;
using System.Text;
using System.Threading.Tasks;

namespace PikaFetcher
{
    internal class Program
    {
        private static async Task Main()
        {
            var program = new Program();
            await program.OnExecuteAsync();
        }

        private Program()
        {
        }

        private async Task OnExecuteAsync()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var api = new PikabuApi();
            await api.Init();

            var random = new RandomFetcher(api);
            var loopTop = new TopFetcher(api, 500, TimeSpan.FromDays(7));
            
            await Task.WhenAll(random.FetchLoop(), loopTop.FetchLoop());
        }
    }
}