using System;
using System.Threading.Tasks;
using SystemAtlas.Core;

namespace SystemAtlas
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--boot-and-collect")
            {
                var orchestrator = new Orchestrator();
                await orchestrator.RunBootAndCollectWorkflowAsync();
            }
            else
            {
                Console.WriteLine("System Atlas");
                Console.WriteLine("Usage: SystemAtlas.exe --boot-and-collect");
            }
        }
    }
}
