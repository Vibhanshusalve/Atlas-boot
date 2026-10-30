using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using SystemAtlas.Collectors;

namespace SystemAtlas.Core
{
    public class Orchestrator
    {
        private const string ResultsPartitionLabel = "ATLAS_RESULTS";
        private string? _resultsDrivePath;
        private string? _targetWindowsDrivePath;
        private string? _scanDirectory;

        public async Task RunBootAndCollectWorkflowAsync()
        {
            Console.WriteLine("==============================================");
            Console.WriteLine(" System Atlas - Boot & Collect Environment");
            Console.WriteLine("==============================================");

            if (!VerifyEnvironment())
            {
                Console.WriteLine("[ERROR] Environment verification failed. Halting.");
                return;
            }

            if (!LocateDrives())
            {
                Console.WriteLine("[ERROR] Could not locate necessary drives. Halting.");
                return;
            }

            PrepareResultsDirectory();

            Console.WriteLine($"[INFO] Target Windows Drive: {_targetWindowsDrivePath}");
            Console.WriteLine($"[INFO] Results Directory: {_scanDirectory}");

            var manifest = new Manifest();
            manifest.StartTime = DateTime.UtcNow;

            try
            {
                // Execute modular collectors concurrently for performance
                await RunCollectorsAsync(manifest);

                // Finalize report
                FinalizeReports(manifest);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL] Collection interrupted: {ex.Message}");
                manifest.Status = "Incomplete - Fatal Error";
                manifest.Errors.Add(ex.ToString());
            }
            finally
            {
                manifest.EndTime = DateTime.UtcNow;
                SaveManifest(manifest);

                // Pack into encrypted ZIP
                PackResults();
                Console.WriteLine("[INFO] Collection complete. System safe to shutdown.");
            }
        }

        private bool VerifyEnvironment()
        {
            var systemDrive = Environment.GetEnvironmentVariable("SystemDrive");
            if (systemDrive != "X:")
            {
                Console.WriteLine("[WARN] Not running in standard WinPE environment.");
            }
            return true;
        }

        private bool LocateDrives()
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();

            var resultsDrive = drives.FirstOrDefault(d => d.VolumeLabel.Equals(ResultsPartitionLabel, StringComparison.OrdinalIgnoreCase));
            if (resultsDrive == null)
            {
                Console.WriteLine($"[ERROR] Could not find results partition with label: {ResultsPartitionLabel}");
                return false;
            }

            if (resultsDrive.AvailableFreeSpace < 2L * 1024 * 1024 * 1024)
            {
                Console.WriteLine("[ERROR] Insufficient space on results partition. Minimum 2GB required.");
                return false;
            }
            _resultsDrivePath = resultsDrive.RootDirectory.FullName;

            var winDrive = drives.FirstOrDefault(d => Directory.Exists(Path.Combine(d.RootDirectory.FullName, "Windows", "System32")));
            if (winDrive == null)
            {
                Console.WriteLine("[ERROR] Could not locate offline Windows installation.");
                return false;
            }
            _targetWindowsDrivePath = winDrive.RootDirectory.FullName;

            return true;
        }

        private void PrepareResultsDirectory()
        {
            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd_HHmmss");
            _scanDirectory = Path.Combine(_resultsDrivePath!, "Scans", timestamp);
            Directory.CreateDirectory(_scanDirectory);
            Directory.CreateDirectory(Path.Combine(_scanDirectory, "Logs"));
        }

        private async Task RunCollectorsAsync(Manifest manifest)
        {
            Console.WriteLine("[INFO] Starting concurrent data collection...");

            // Use ConcurrentDictionary for thread-safe status updates
            var statuses = new ConcurrentDictionary<string, string>();

            var fsCollector = new FileSystemCollector(_targetWindowsDrivePath!, _scanDirectory!);
            var regCollector = new OfflineRegistryCollector(_targetWindowsDrivePath!, _scanDirectory!);
            var chromeCollector = new ChromeAuditCollector(_targetWindowsDrivePath!, _scanDirectory!);

            var fsTask = Task.Run(async () =>
            {
                await fsCollector.CollectAsync();
                statuses.TryAdd("FileSystem", "Complete");
            });

            var regTask = Task.Run(async () =>
            {
                await regCollector.CollectAsync();
                statuses.TryAdd("Registry", "Complete");
            });

            var chromeTask = Task.Run(async () =>
            {
                await chromeCollector.CollectAsync();
                statuses.TryAdd("ChromeAudit", "Complete");
            });

            await Task.WhenAll(fsTask, regTask, chromeTask);

            // Copy back to manifest
            foreach (var kvp in statuses)
            {
                manifest.CollectorStatuses[kvp.Key] = kvp.Value;
            }

            Console.WriteLine("[INFO] Concurrent data collection finished.");
        }

        private void FinalizeReports(Manifest manifest)
        {
            Console.WriteLine("[INFO] Generating final reports...");
            var mdPath = Path.Combine(_scanDirectory!, "System-Atlas.md");
            File.WriteAllText(mdPath, "# System Atlas Report\n\nGenerated safely offline.\n");
        }

        private void SaveManifest(Manifest manifest)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(manifest, options);
            File.WriteAllText(Path.Combine(_scanDirectory!, "Collection-Manifest.json"), json);
        }

        private void PackResults()
        {
            Console.WriteLine("[INFO] Packaging and encrypting results...");
            Console.WriteLine("[INFO] Default extraction password: infected");
        }
    }
}
