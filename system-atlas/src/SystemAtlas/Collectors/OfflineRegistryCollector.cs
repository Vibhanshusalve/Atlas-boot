using System;
using System.IO;
using System.Threading.Tasks;

namespace SystemAtlas.Collectors
{
    public class OfflineRegistryCollector
    {
        private readonly string _targetDrive;
        private readonly string _scanDirectory;

        public OfflineRegistryCollector(string targetDrive, string scanDirectory)
        {
            _targetDrive = targetDrive;
            _scanDirectory = scanDirectory;
        }

        public async Task CollectAsync()
        {
            // In a full implementation, you would use a library like RegistryCore or
            // discutils to parse the offline hives. Here we catalog their presence.
            var mdPath = Path.Combine(_scanDirectory, "System-Atlas.md");
            using var writer = new StreamWriter(mdPath, append: true);

            await writer.WriteLineAsync("\n## Offline Registry Hives");

            var configDir = Path.Combine(_targetDrive, "Windows", "System32", "config");
            if (Directory.Exists(configDir))
            {
                var hives = new[] { "SYSTEM", "SOFTWARE", "SAM", "SECURITY", "DEFAULT" };
                foreach (var hive in hives)
                {
                    var hivePath = Path.Combine(configDir, hive);
                    if (File.Exists(hivePath))
                    {
                        var info = new FileInfo(hivePath);
                        await writer.WriteLineAsync($"- **{hive}**: Found ({info.Length / 1024} KB), Last Modified: {info.LastWriteTimeUtc:O}");
                    }
                    else
                    {
                        await writer.WriteLineAsync($"- **{hive}**: Not found");
                    }
                }
            }
        }
    }
}
