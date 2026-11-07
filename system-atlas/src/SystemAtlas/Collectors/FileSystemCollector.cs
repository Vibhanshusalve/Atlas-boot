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

                // Use EnumerateFileSystemInfos to avoid iterating the directory twice (once for files, once for directories)
                foreach (var item in dirInfo.EnumerateFileSystemInfos("*", new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    ReturnSpecialDirectories = false
                }))
                {
                    if (item is FileInfo file)
                    {
                        // Do not collect file contents. Just metadata.
                        var line = $"\"{file.FullName}\",{file.Length},{file.CreationTimeUtc:O},{file.LastWriteTimeUtc:O},\"{file.Attributes}\"";
                        await writer.WriteLineAsync(line);
                    }
                    else if (item is DirectoryInfo subDir)
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
