using System;
using System.IO;
using System.Threading.Tasks;

namespace SystemAtlas.Collectors
{
    public class ChromeAuditCollector
    {
        // ⚡ Bolt Optimization: Use static EnumerationOptions to avoid allocating new configuration objects
        // during directory enumeration. This reduces unnecessary garbage collection overhead.
        private static readonly EnumerationOptions s_dirEnumerationOptions = new EnumerationOptions
        {
            IgnoreInaccessible = true
        };

        private readonly string _targetDrive;
        private readonly string _scanDirectory;

        public ChromeAuditCollector(string targetDrive, string scanDirectory)
        {
            _targetDrive = targetDrive;
            _scanDirectory = scanDirectory;
        }

        public async Task CollectAsync()
        {
            var auditPath = Path.Combine(_scanDirectory, "Browser-Profile-Audit.md");
            using var writer = new StreamWriter(auditPath, append: false);

            await writer.WriteLineAsync("# Chrome Profile Audit");
            await writer.WriteLineAsync("\n*Note: This audit identifies recovery readiness. It does not extract passwords or bypass encryption.*");

            var usersDir = Path.Combine(_targetDrive, "Users");
            if (!Directory.Exists(usersDir)) return;

            foreach (var userDir in Directory.EnumerateDirectories(usersDir, "*", s_dirEnumerationOptions))
            {
                var chromeDataPath = Path.Combine(userDir, "AppData", "Local", "Google", "Chrome", "User Data");
                if (Directory.Exists(chromeDataPath))
                {
                    await writer.WriteLineAsync($"\n## Found Chrome Data for User: {Path.GetFileName(userDir)}");
                    await AuditProfileAsync(chromeDataPath, "Default", writer);

                    // Audit other profiles (Profile 1, Profile 2, etc.)
                    foreach (var profileDir in Directory.EnumerateDirectories(chromeDataPath, "Profile *", SearchOption.TopDirectoryOnly))
                    {
                        await AuditProfileAsync(chromeDataPath, Path.GetFileName(profileDir), writer);
                    }
                }
            }
        }

        private async Task AuditProfileAsync(string baseDataPath, string profileName, StreamWriter writer)
        {
            var profilePath = Path.Combine(baseDataPath, profileName);
            var localStatePath = Path.Combine(baseDataPath, "Local State");
            var loginDataPath = Path.Combine(profilePath, "Login Data");

            await writer.WriteLineAsync($"\n### Profile: {profileName}");

            bool hasLocalState = File.Exists(localStatePath);
            bool hasLoginData = File.Exists(loginDataPath);

            await writer.WriteLineAsync($"- **Local State Present**: {hasLocalState}");
            await writer.WriteLineAsync($"- **Login Data Present**: {hasLoginData}");

            if (hasLocalState && hasLoginData)
            {
                await writer.WriteLineAsync("\n#### Recovery Assessment:");
                await writer.WriteLineAsync("The necessary files for decryption (Local State containing the DPAPI encrypted master key, and the Login Data SQLite database) are present on disk.");
                await writer.WriteLineAsync("Because we are operating offline and read-only, **no credentials have been extracted**.");
                await writer.WriteLineAsync("To recover these passwords, the offline registry hives (SYSTEM/SAM/SECURITY) must be utilized to decrypt the DPAPI master key, or the files must be analyzed within the original user's context.");
                await writer.WriteLineAsync("Application-Bound Encryption (App-Bound) may further complicate offline recovery in recent Chrome versions.");
            }
        }
    }
}
