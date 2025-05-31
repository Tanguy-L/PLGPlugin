using CounterStrikeSharp.API;
using PLGPlugin.Interfaces;
using System.Text.RegularExpressions;

namespace PLGPlugin
{
    public class BackupManager
    {
        private readonly ILoggingService _logger;
        private readonly string _backupDirectory;
        private readonly string _prefixFilename = "plg";
        private List<BackupFile> _cachedBackups = [];
        private string? _matchId;
        // private bool _disposed;

        public BackupManager(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _backupDirectory = Path.Combine(Server.GameDirectory, "csgo");
        }

        public void SetMatchId(string matchId)
        {
            _matchId = matchId;
        }

        public void SetStandardBackup()
        {
            Server.ExecuteCommand($"mp_backup_round_file {_prefixFilename}");
            Server.ExecuteCommand("mp_backup_round_file_pattern %prefix%_%date%_%time%_%team1%_%team2%_%map%_round%round%_score_%score1%_%score2%.txt");
        }

        public void SetBackupPLG(string matchId, string teamName1, string teamName2)
        {
            Server.ExecuteCommand($"mp_backup_round_file {_prefixFilename}_{matchId}_{teamName1}_{teamName2}");
            Server.ExecuteCommand("mp_backup_round_file_pattern %prefix%_%score1%_%score2%.txt");
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
                    .Where(FilterBackupFile)
                    .OrderByDescending(b => b.CreatedTime)
                    .ToList();

                _cachedBackups = backupFiles;
                _logger.Info($"Refreshed backup cache with {_cachedBackups.Count} files");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error refreshing backup cache: {ex.Message}", ex);
            }
        }

        private bool FilterBackupFile(BackupFile backup)
        {
            if (!string.IsNullOrEmpty(_matchId))
            {
                return backup.MatchNumber?.ToString() == _matchId;
            }

            string today = DateTime.Now.ToString("yyyyMMdd");
            return backup.Date == today || backup.CreatedTime.Date == DateTime.Today;
        }

        private BackupFile? ParseBackupFile(string filePath)
        {
            try
            {
                string fileName = Path.GetFileName(filePath);

                // Parse PLG backup filename: plg_{number}_{team1}_{team2}_{score1}_{score2}.txt
                Match match = Regex.Match(fileName,
                    @"^plg_(\d+)_(.+?)_(.+?)_(\d+)_(\d+)\.txt$",
                    RegexOptions.IgnoreCase);

                if (!match.Success)
                {
                    // Fallback: try the old format for backward compatibility
                    // plg_YYYYMMDD_mapname_roundXX.txt or plg_YYYYMMDD_mapname.txt
                    Match oldMatch = Regex.Match(fileName,
                        @"^plg_(\d{8})_(.+?)(?:_round(\d+))?\.txt$",
                        RegexOptions.IgnoreCase);

                    if (!oldMatch.Success)
                    {
                        return null;
                    }

                    // Handle old format
                    string date = oldMatch.Groups[1].Value;
                    string map = oldMatch.Groups[2].Value;
                    int round = oldMatch.Groups[3].Success ? int.Parse(oldMatch.Groups[3].Value) : 0;
                    FileInfo fileAllInfos = new(filePath);

                    return new BackupFile
                    {
                        FileName = fileName,
                        FullPath = filePath,
                        Date = date,
                        Map = map,
                        Round = round,
                        CreatedTime = fileAllInfos.CreationTime,
                        FileSize = fileAllInfos.Length,
                        // For old format, we don't have team/score info
                        Team1 = null,
                        Team2 = null,
                        Score1 = null,
                        Score2 = null,
                        MatchNumber = null
                    };
                }

                // Handle new format: plg_{number}_{team1}_{team2}_{score1}_{score2}.txt
                int matchNumber = int.Parse(match.Groups[1].Value);
                string team1 = match.Groups[2].Value;
                string team2 = match.Groups[3].Value;
                int score1 = int.Parse(match.Groups[4].Value);
                int score2 = int.Parse(match.Groups[5].Value);

                FileInfo fileInfo = new(filePath);

                return new BackupFile
                {
                    FileName = fileName,
                    FullPath = filePath,
                    MatchNumber = matchNumber,
                    Team1 = team1,
                    Team2 = team2,
                    Score1 = score1,
                    Score2 = score2,
                    CreatedTime = fileInfo.CreationTime,
                    FileSize = fileInfo.Length,
                    // For new format, we don't have explicit date/map/round
                    Date = null,
                    Map = null,
                    Round = null
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

        public List<BackupFile> GetLastPLGBackups(string matchId)
        {

            return [.. _cachedBackups.Where(file => file.MatchNumber.ToString() == matchId)];
        }

        public void RestoreMostRecent()
        {
            if (_cachedBackups.Count > 0)
            {
                BackupFile mostRecent = _cachedBackups[0];
                Server.ExecuteCommand($"mp_backup_restore_load_file {mostRecent.FileName}");
                _logger.Info($"Restored most recent backup: {mostRecent.FileName}");
            }
            else
            {
                _logger.Warning("No backup files available to restore");
            }
        }

        public void RestoreAtIndex(int index)
        {
            if (index >= 0 && index < _cachedBackups.Count)
            {
                BackupFile backup = _cachedBackups[index];
                Server.ExecuteCommand($"mp_backup_restore_load_file {backup.FileName}");
                _logger.Info($"Restored backup at index {index}: {backup.FileName}");
            }
            else
            {
                _logger.Warning($"Invalid backup index: {index}. Available range: 0-{_cachedBackups.Count - 1}");
            }
        }

        public BackupManager() : this(LoggingService.Instance)
        {
        }
    }

    public class BackupFile
    {
        public required string FileName { get; set; }
        public required string FullPath { get; set; }
        public DateTime CreatedTime { get; set; }
        public long FileSize { get; set; }

        // Original format properties (plg_YYYYMMDD_mapname_roundXX.txt)
        public string? Date { get; set; }
        public string? Map { get; set; }
        public int? Round { get; set; }

        // New format properties (plg_{number}_{team1}_{team2}_{score1}_{score2}.txt)
        public int? MatchNumber { get; set; }
        public string? Team1 { get; set; }
        public string? Team2 { get; set; }
        public int? Score1 { get; set; }
        public int? Score2 { get; set; }

        // Helper properties for display
        public bool IsNewFormat => MatchNumber.HasValue;
        public bool IsOldFormat => !string.IsNullOrEmpty(Date);

        public string DisplayName => IsNewFormat
            ? $"[{MatchNumber}]: {Team1} vs {Team2} ({Score1}-{Score2})"
            : $"{Date} - {Map}" + (Round > 0 ? $" (Round {Round})" : "");
    }
}
