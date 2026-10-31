using System;
using System.IO;
using System.IO.Enumeration;
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
                    // ⚡ Bolt: Offload synchronous I/O and low-level FileSystemEnumerable to thread pool
                    // to prevent async state machine overhead for every single file.
                    await Task.Run(() => EnumerateDirectory(usersDir, writer));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] FileSystemCollector failed: {ex.Message}");
            }
        }

        private void EnumerateDirectory(string dirPath, StreamWriter writer)
        {
            try
            {
                var dirInfo = new DirectoryInfo(dirPath);

                // Prevent following reparse points (symlinks/junctions) to avoid loops at root level
                if (dirInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
                {
                    return;
                }

                var options = new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    ReturnSpecialDirectories = false,
                    RecurseSubdirectories = true,
                    AttributesToSkip = 0 // Do not skip hidden/system files by default
                };

                // ⚡ Bolt: Using FileSystemEnumerable is significantly faster and allocates less memory
                // than DirectoryInfo.EnumerateFiles because it bypasses creating FileInfo objects.
                var enumerable = new FileSystemEnumerable<string>(
                    dirPath,
                    (ref FileSystemEntry entry) =>
                    {
                        return $"\"{entry.ToFullPath()}\",{entry.Length},{entry.CreationTimeUtc:O},{entry.LastWriteTimeUtc:O},\"{entry.Attributes}\"";
                    },
                    options
                )
                {
                    ShouldIncludePredicate = (ref FileSystemEntry entry) => !entry.IsDirectory,
                    ShouldRecursePredicate = (ref FileSystemEntry entry) => (entry.Attributes & FileAttributes.ReparsePoint) == 0
                };

                foreach (var line in enumerable)
                {
                    writer.WriteLine(line);
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
