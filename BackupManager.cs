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
        // private List<BackupFile> _cachedBackups = [];
        private bool _disposed;

        public BackupManager(ILoggingService logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _backupDirectory = Path.Combine(Server.GameDirectory, "csgo");
            // RefreshBackupCache();
        }

        public BackupManager() : this(LoggingService.Instance)
        {
        }
    }
}

