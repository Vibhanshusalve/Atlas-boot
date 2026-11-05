using System;
using System.IO;
using System.Threading.Tasks;

namespace SystemAtlas.Collectors
{
    public class FileSystemCollector
    {
        private readonly string _targetDrive;
        private readonly string _scanDirectory;

        // ⚡ Bolt: Single reusable EnumerationOptions to avoid allocation per directory
        private static readonly EnumerationOptions _enumOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true,
            ReturnSpecialDirectories = false
        };

        public FileSystemCollector(string targetDrive, string scanDirectory)
        {
            _targetDrive = targetDrive;
            _scanDirectory = scanDirectory;
        }

        public async Task CollectAsync()
        {
            var csvPath = Path.Combine(_scanDirectory, "File-Inventory.csv");

            // Using a StreamWriter with transactional/incremental writes.
            using var writer = new StreamWriter(csvPath, append: false);
            await writer.WriteLineAsync("Path,Size,CreationTime,LastWriteTime,Attributes");

            try
            {
                // Start enumerating from Users and select important roots to avoid scanning C:\Windows entirely unless needed.
                var usersDir = Path.Combine(_targetDrive, "Users");
                if (Directory.Exists(usersDir))
                {
                    await EnumerateDirectoryAsync(usersDir, writer);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] FileSystemCollector failed: {ex.Message}");
            }
        }

        private async Task EnumerateDirectoryAsync(string dirPath, StreamWriter writer)
        {
            try
            {
                var dirInfo = new DirectoryInfo(dirPath);

                // Prevent following reparse points (symlinks/junctions) to avoid loops
                if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    return;
                }

                // ⚡ Bolt: Combined file/directory enumeration into a single pass (EnumerateFileSystemInfos)
                // This halves the number of OS-level directory queries and dramatically improves performance,
                // especially on slow media or drives with high file counts.
                foreach (var entry in dirInfo.EnumerateFileSystemInfos("*", _enumOptions))
                {
                    if (entry is FileInfo file)
                    {
                        // Do not collect file contents. Just metadata.
                        var line = $"\"{file.FullName}\",{file.Length},{file.CreationTimeUtc:O},{file.LastWriteTimeUtc:O},\"{file.Attributes}\"";
                        await writer.WriteLineAsync(line);
                    }
                    else if (entry is DirectoryInfo subDir)
                    {
                        await EnumerateDirectoryAsync(subDir.FullName, writer);
                    }
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
