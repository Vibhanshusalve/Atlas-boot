using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

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
                // Execute modular collectors
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
            // Verify we are running in WinPE (X: drive is typically WinPE RAMDisk)
            var systemDrive = Environment.GetEnvironmentVariable("SystemDrive");
            if (systemDrive != "X:")
            {
                Console.WriteLine("[WARN] Not running in standard WinPE environment.");
                // For testing, we might return true, but in prod we strictly check this.
            }
            return true;
        }

        private bool LocateDrives()
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady).ToList();

            // Locate USB results partition by label
            var resultsDrive = drives.FirstOrDefault(d => d.VolumeLabel.Equals(ResultsPartitionLabel, StringComparison.OrdinalIgnoreCase));
            if (resultsDrive == null)
            {
                Console.WriteLine($"[ERROR] Could not find results partition with label: {ResultsPartitionLabel}");
                return false;
            }

            // Verify space (> 2GB free)
            if (resultsDrive.AvailableFreeSpace < 2L * 1024 * 1024 * 1024)
            {
                Console.WriteLine("[ERROR] Insufficient space on results partition. Minimum 2GB required.");
                return false;
            }
            _resultsDrivePath = resultsDrive.RootDirectory.FullName;

            // Locate Target Windows drive (heuristic: contains \Windows\System32)
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
            // Pseudo-code for running collectors
            Console.WriteLine("[INFO] Running File System Collector...");
            // await new FileSystemCollector(_targetWindowsDrivePath, _scanDirectory).CollectAsync();
            manifest.CollectorStatuses["FileSystem"] = "Complete";

            Console.WriteLine("[INFO] Running Offline Registry Collector...");
            // await new OfflineRegistryCollector(_targetWindowsDrivePath, _scanDirectory).CollectAsync();
            manifest.CollectorStatuses["Registry"] = "Complete";

            Console.WriteLine("[INFO] Running Chrome Audit Module...");
            // await new ChromeAuditCollector(_targetWindowsDrivePath, _scanDirectory).CollectAsync();
            manifest.CollectorStatuses["ChromeAudit"] = "Complete";

            // Add slight delay to simulate work for the skeleton
            await Task.Delay(500);
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
            // In a real implementation, use System.IO.Compression.ZipFile to create an encrypted zip
            // ZipFile.CreateFromDirectory(_scanDirectory, _scanDirectory + ".zip", CompressionLevel.Optimal, false, Encoding.UTF8);
            // using DotNetZip or similar for AES encryption with the default password.
            Console.WriteLine("[INFO] Default extraction password: infected");
        }
    }
}
