using System;
using System.Threading.Tasks;

namespace DBot
{
    public class Program
    {
        static void Main(string[] args)
        => new Program().StartAsync().GetAwaiter().GetResult();

        public async Task StartAsync()
        {
            await new DBot.Source.DBot().RunAndBlockAsync();
        }
    }
}
