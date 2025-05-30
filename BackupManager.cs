using CounterStrikeSharp.API;
using PLGPlugin.Interfaces;
using System.Text.RegularExpressions;

namespace PLGPlugin
{
    public class BackupManager
    {
        private readonly ILoggingService _logger;
        private readonly string _backupDirectory;
        private readonly string _prefixFilename = "plg_";
        private List<BackupFile> _cachedBackups = [];
        // private bool _disposed;

        public BackupManager(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _backupDirectory = Path.Combine(Server.GameDirectory, "csgo");
            DateTime date = DateTime.Now;
            string dateFormatted = date.ToString("dd/M/yyyy");
            Server.ExecuteCommand($"mp_backup_round_file {_prefixFilename}_{dateFormatted}");
        }

        public void SetBackupPLG(string matchId, string teamName1, string teamName2)
        {

            Server.ExecuteCommand($"mp_backup_round_file {_prefixFilename}_{matchId}_{teamName1}_{teamName2}");
        }

        /// <summary>
        /// Refreshes the backup cache by scanning the backup directory
        /// </summary>
        public void RefreshBackupCache()
        {
            try
            {
                _cachedBackups.Clear();

                if (!Directory.Exists(_backupDirectory))
                {
                    _logger.Warning($"Backup directory does not exist: {_backupDirectory}");
                    return;
                }

                List<BackupFile> backupFiles = Directory.GetFiles(_backupDirectory, "*.txt")
                    .Where(f => Path.GetFileName(f).StartsWith(_prefixFilename))
                    .Select(ParseBackupFile)
                    .Where(b => b != null)
                    .Cast<BackupFile>()
                    .OrderBy(b => b.CreatedTime)
                    .ToList();

                _cachedBackups = backupFiles;
                _logger.Info($"Refreshed backup cache with {_cachedBackups.Count} files");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error refreshing backup cache: {ex.Message}", ex);
            }
        }

        private BackupFile? ParseBackupFile(string filePath)
        {
            try
            {
                string fileName = Path.GetFileName(filePath);

                // Parse PLG backup filename: plg_YYYYMMDD_mapname_roundXX.txt or plg_YYYYMMDD_mapname.txt
                Match match = Regex.Match(fileName,
                    @"^plg_(\d{8})_(.+?)(?:_round(\d+))?\.txt$",
                    RegexOptions.IgnoreCase);

                if (!match.Success)
                {
                    return null;
                }

                string date = match.Groups[1].Value;
                string map = match.Groups[2].Value;
                int round = match.Groups[3].Success ? int.Parse(match.Groups[3].Value) : 0;

                FileInfo fileInfo = new(filePath);

                return new BackupFile
                {
                    FileName = fileName,
                    FullPath = filePath,
                    Date = date,
                    Map = map,
                    Round = round,
                    CreatedTime = fileInfo.CreationTime,
                    FileSize = fileInfo.Length
                };
            }
            catch (Exception ex)
            {
                _logger.Warning($"Failed to parse backup file {filePath}: {ex.Message}");
                return null;
            }
        }

        public List<BackupFile> GetLastBackups()
        {
            return _cachedBackups;
        }

        public BackupManager() : this(LoggingService.Instance)
        {
        }
    }

    public class BackupFile
    {
        public required string FileName { get; set; }
        public required string FullPath { get; set; }
        public required string Date { get; set; }
        public required string Map { get; set; }
        public int Round { get; set; }
        public DateTime CreatedTime { get; set; }
        public long FileSize { get; set; }

        public string FormattedSize => FormatFileSize(FileSize);
        public string FormattedDate => CreatedTime.ToString("yyyy-MM-dd HH:mm:ss");

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double size = bytes;
            int order = 0;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
}

