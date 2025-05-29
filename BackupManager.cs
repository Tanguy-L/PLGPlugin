using CounterStrikeSharp.API;
using PLGPlugin.Interfaces;
using System.Text.RegularExpressions;

namespace PLGPlugin
{
    public class BackupManager : IDisposable
    {
        private readonly ILoggingService _logger;
        private readonly string _backupDirectory;
        private readonly string _prefixFilename = "plg_";
        private List<BackupFile> _cachedBackups = [];
        private bool _disposed;

        public BackupManager(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _backupDirectory = Path.Combine(Server.GameDirectory, "csgo/backups");
            RefreshBackupCache();
        }

        public BackupManager() : this(LoggingService.Instance)
        {
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(BackupManager));
        }

        /// <summary>
        /// Formats a backup filename with the PLG prefix
        /// </summary>
        public string FormatFilename(string date, string map, int round = 0)
        {
            ThrowIfDisposed();

            string baseFilename = round > 0
                ? $"{_prefixFilename}{date}_{map}_round{round:D2}.txt"
                : $"{_prefixFilename}{date}_{map}.txt";

            return baseFilename;
        }

        /// <summary>
        /// Gets a backup filename by index (inverted - most recent first)
        /// </summary>
        public string? GetFilenameByIndex(int index)
        {
            ThrowIfDisposed();

            if (_cachedBackups.Count == 0 || index < 0 || index >= _cachedBackups.Count)
            {
                return null;
            }

            // Invert index to get most recent first
            int actualIndex = _cachedBackups.Count - 1 - index;
            return _cachedBackups[actualIndex].FileName;
        }

        /// <summary>
        /// Adds a backup filename to the cache
        /// </summary>
        public void AddBackupFilename(string date, string map, int round = 0)
        {
            ThrowIfDisposed();

            string filename = FormatFilename(date, map, round);
            string fullPath = Path.Combine(_backupDirectory, filename);

            if (File.Exists(fullPath))
            {
                BackupFile backupFile = new()
                {
                    FileName = filename,
                    FullPath = fullPath,
                    Date = date,
                    Map = map,
                    Round = round,
                    CreatedTime = File.GetCreationTime(fullPath),
                    FileSize = new FileInfo(fullPath).Length
                };

                _cachedBackups.Add(backupFile);
                _cachedBackups = _cachedBackups.OrderBy(b => b.CreatedTime).ToList();

                _logger.Info($"Added backup to cache: {filename}");
            }
        }

        /// <summary>
        /// Refreshes the backup cache by scanning the backup directory
        /// </summary>
        public void RefreshBackupCache()
        {
            ThrowIfDisposed();

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

        /// <summary>
        /// Gets the most recent backup files for a specific map
        /// </summary>
        public List<BackupFile> GetRecentBackups(string? mapName = null, int count = 5)
        {
            ThrowIfDisposed();

            IEnumerable<BackupFile> query = _cachedBackups.AsEnumerable();

            if (!string.IsNullOrEmpty(mapName))
            {
                query = query.Where(b => b.Map.Equals(mapName, StringComparison.OrdinalIgnoreCase));
            }

            return [.. query
                    .OrderByDescending(b => b.CreatedTime)
                    .Take(count)];
        }

        /// <summary>
        /// Gets backup files for today's date and specified map
        /// </summary>
        public List<BackupFile> GetTodaysBackups(string mapName)
        {
            ThrowIfDisposed();

            string todayDate = DateTime.Now.ToString("yyyyMMdd");

            return [.. _cachedBackups
                    .Where(b => b.Date == todayDate &&
                               b.Map.Equals(mapName, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(b => b.CreatedTime)];
        }

        /// <summary>
        /// Creates a manual backup with current timestamp
        /// </summary>
        public string CreateManualBackup(string reason = "manual")
        {
            ThrowIfDisposed();

            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string mapName = Server.MapName;
                string filename = $"{_prefixFilename}{timestamp}_{mapName}_{reason}.txt";

                // Execute CS2 backup command
                Server.ExecuteCommand($"mp_backup_round_file {filename}");

                _logger.Info($"Created manual backup: {filename}");

                // Add to cache after a short delay to ensure file is created
                _ = Task.Delay(1000).ContinueWith(_ =>
                {
                    Server.NextFrame(() => AddBackupFilename(timestamp, mapName));
                });

                return filename;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error creating manual backup: {ex.Message}", ex);
                throw;
            }
        }

        /// <summary>
        /// Restores a backup file
        /// </summary>
        public bool RestoreBackup(string filename)
        {
            ThrowIfDisposed();

            try
            {
                // Ensure filename doesn't have path traversal
                filename = Path.GetFileName(filename);

                BackupFile? backupFile = _cachedBackups.FirstOrDefault(b =>
                    b.FileName.Equals(filename, StringComparison.OrdinalIgnoreCase));

                if (backupFile == null)
                {
                    _logger.Warning($"Backup file not found in cache: {filename}");
                    return false;
                }

                if (!File.Exists(backupFile.FullPath))
                {
                    _logger.Error($"Backup file does not exist on disk: {backupFile.FullPath}");
                    return false;
                }

                // Execute CS2 restore command
                Server.ExecuteCommand($"mp_backup_restore_load_file {filename}");

                _logger.Info($"Restored backup: {filename}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.Error($"Error restoring backup {filename}: {ex.Message}", ex);
                return false;
            }
        }

        /// <summary>
        /// Cleans up old backup files based on age and count limits
        /// </summary>
        public void CleanupOldBackups(int maxFiles = 50, int maxDaysOld = 30)
        {
            ThrowIfDisposed();

            try
            {
                List<BackupFile> filesToDelete = [];

                // Find files older than maxDaysOld
                List<BackupFile> oldFiles = _cachedBackups
                    .Where(b => b.CreatedTime < DateTime.Now.AddDays(-maxDaysOld))
                    .ToList();

                filesToDelete.AddRange(oldFiles);

                // If we still have too many files, remove oldest ones
                List<BackupFile> remainingFiles = _cachedBackups.Except(oldFiles).ToList();
                if (remainingFiles.Count > maxFiles)
                {
                    IEnumerable<BackupFile> excessFiles = remainingFiles
                        .OrderBy(b => b.CreatedTime)
                        .Take(remainingFiles.Count - maxFiles);

                    filesToDelete.AddRange(excessFiles);
                }

                // Delete the files
                foreach (BackupFile? file in filesToDelete.Distinct())
                {
                    try
                    {
                        if (File.Exists(file.FullPath))
                        {
                            File.Delete(file.FullPath);
                            _ = _cachedBackups.Remove(file);
                            _logger.Debug($"Deleted old backup: {file.FileName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning($"Failed to delete backup file {file.FileName}: {ex.Message}");
                    }
                }

                if (filesToDelete.Count != 0)
                {
                    _logger.Info($"Cleaned up {filesToDelete.Count} old backup files");
                }
            }
            catch (Exception ex)
            {
                _logger.Error($"Error during backup cleanup: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Gets backup statistics
        /// </summary>
        public BackupStats GetBackupStats()
        {
            ThrowIfDisposed();

            return new BackupStats
            {
                TotalBackups = _cachedBackups.Count,
                TotalSizeBytes = _cachedBackups.Sum(b => b.FileSize),
                OldestBackup = _cachedBackups.MinBy(b => b.CreatedTime)?.CreatedTime,
                NewestBackup = _cachedBackups.MaxBy(b => b.CreatedTime)?.CreatedTime,
                BackupsByMap = _cachedBackups
                    .GroupBy(b => b.Map)
                    .ToDictionary(g => g.Key, g => g.Count())
            };
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

        public void Dispose()
        {
            if (!_disposed)
            {
                _cachedBackups.Clear();
                _logger?.Info("BackupManager disposed");
                _disposed = true;
            }
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
            string[] sizes = ["B", "KB", "MB", "GB"];
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

    public class BackupStats
    {
        public int TotalBackups { get; set; }
        public long TotalSizeBytes { get; set; }
        public DateTime? OldestBackup { get; set; }
        public DateTime? NewestBackup { get; set; }
        public Dictionary<string, int> BackupsByMap { get; set; } = new();

        public string FormattedTotalSize => FormatFileSize(TotalSizeBytes);

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
