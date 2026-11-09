using System;
using System.IO;
using System.Threading.Tasks;

namespace SystemAtlas.Collectors
{
    public class FileSystemCollector
    {
        private readonly string _targetDrive;
        private readonly string _scanDirectory;

        public FileSystemCollector(string targetDrive, string scanDirectory)
        {
            _targetDrive = targetDrive;
            _scanDirectory = scanDirectory;
        }

        public async Task CollectAsync()
        {
            var csvPath = Path.Combine(_scanDirectory, "File-Inventory.csv");

            // Optimization: Wrap the synchronous I/O in Task.Run to prevent blocking the calling thread.
            await Task.Run(() =>
            {
                // Optimization: Using a larger buffer size (64KB) for StreamWriter to batch file I/O writes.
                // Using synchronous WriteLine instead of WriteLineAsync to avoid Task allocation and state machine
                // overhead for every single file enumerated, which is significant for high-frequency fine-grained operations.
                using var writer = new StreamWriter(csvPath, append: false, encoding: System.Text.Encoding.UTF8, bufferSize: 65536);
                writer.WriteLine("Path,Size,CreationTime,LastWriteTime,Attributes");

                try
                {
                    // Start enumerating from Users and select important roots to avoid scanning C:\Windows entirely unless needed.
                    var usersDir = Path.Combine(_targetDrive, "Users");
                    if (Directory.Exists(usersDir))
                    {
                        EnumerateDirectory(usersDir, writer);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] FileSystemCollector failed: {ex.Message}");
                }
            });
        }

        private void EnumerateDirectory(string dirPath, StreamWriter writer)
        {
            try
            {
                var dirInfo = new DirectoryInfo(dirPath);

                // Prevent following reparse points (symlinks/junctions) to avoid loops
                if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    return;
                }

                foreach (var file in dirInfo.EnumerateFiles("*", new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    ReturnSpecialDirectories = false
                }))
                {
                    // Do not collect file contents. Just metadata.
                    var line = $"\"{file.FullName}\",{file.Length},{file.CreationTimeUtc:O},{file.LastWriteTimeUtc:O},\"{file.Attributes}\"";
                    writer.WriteLine(line);
                }

                foreach (var subDir in dirInfo.EnumerateDirectories("*", new EnumerationOptions { IgnoreInaccessible = true }))
                {
                    EnumerateDirectory(subDir.FullName, writer);
                }
            }
            catch (UnauthorizedAccessException)
            {
                // Explicitly record inaccessible paths in a separate log if required, or skip gracefully.
            }
            catch (Exception)
            {
                // Log other enumeration errors
            }
        }
    }
}
