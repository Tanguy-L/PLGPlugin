using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API.Core;
using PLGPlugin.Interfaces;

namespace PLGPlugin
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    public class LoggingService : ILoggingService
    {
        private readonly ILogger _logger;
        private readonly bool _printToConsole;
        private readonly bool _printToChat;
        private readonly bool _printToServer;
        private readonly string _pluginPrefix;
        private readonly string _chatPrefix;
        private readonly string _adminPrefix;

        // Singleton instance
        private static LoggingService? _instance;

        // Constants for reuse
        public static readonly string DefaultChatPrefix =
            $"[{ChatColors.Blue}P{ChatColors.Yellow}L{ChatColors.Red}G{ChatColors.Default}]";
        public static readonly string DefaultAdminPrefix =
            $"[{ChatColors.Red}ADMIN{ChatColors.Default}]";

        public LoggingService(ILogger logger, bool printToConsole = true, bool printToChat = false,
                             bool printToServer = true, string pluginPrefix = "[PLG] ")
        {
            _logger = logger;
            _printToConsole = printToConsole;
            _printToChat = printToChat;
            _printToServer = printToServer;
            _pluginPrefix = pluginPrefix;
            _chatPrefix = DefaultChatPrefix;
            _adminPrefix = DefaultAdminPrefix;
        }

        // Set up the singleton instance
        public static void Initialize(ILogger logger, bool printToConsole = true, bool printToChat = false,
                                     bool printToServer = true, string pluginPrefix = "[PLG] ")
        {
            _instance = new LoggingService(logger, printToConsole, printToChat, printToServer, pluginPrefix);
        }

        // Access the singleton
        public static LoggingService Instance => _instance ?? throw new InvalidOperationException("LoggingService must be initialized before use");

        public void Log(string message, LogLevel level = LogLevel.Info)
        {
            string prefixedMessage = $"{_pluginPrefix} : {message}";

            // Log to the logger
            switch (level)
            {
                case LogLevel.Debug:
                    _logger.LogDebug(prefixedMessage);
                    break;
                case LogLevel.Info:
                    _logger.LogInformation(prefixedMessage);
                    break;
                case LogLevel.Warning:
                    _logger.LogWarning(prefixedMessage);
                    break;
                case LogLevel.Error:
                    _logger.LogError(prefixedMessage);
                    break;
                case LogLevel.Critical:
                    _logger.LogCritical(prefixedMessage);
                    break;
                default:
                    break;
            }

            // Print to console
            if (_printToConsole)
            {
                Console.WriteLine(prefixedMessage);
            }

            // Print to server
            if (_printToServer)
            {
                Server.PrintToConsole(prefixedMessage);
            }
        }

        public void Debug(string message)
        {
            Log(message, LogLevel.Debug);
        }

        public void Info(string message)
        {
            Log(message, LogLevel.Info);
        }

        public void Warning(string message)
        {
            Log(message, LogLevel.Warning);
        }

        public void Error(string message)
        {
            Log(message, LogLevel.Error);
        }

        public void Critical(string message)
        {
            Log(message, LogLevel.Critical);
        }

        // Chat-specific methods
        public void ChatToAll(string message)
        {
            if (_printToChat)
            {
                Server.PrintToChatAll($"{_chatPrefix} {message}");
            }

            // Also log to normal logging system
            Info($"[CHAT_ALL] {message}");
        }

        public void ChatToPlayer(CCSPlayerController player, string message)
        {
            if (_printToChat && player != null && player.IsValid)
            {
                player.PrintToChat($"{_chatPrefix} {message}");
            }

            // Also log to normal logging system
            Info($"[CHAT_PLAYER:{player?.PlayerName ?? "Unknown"}] {message}");
        }

        public void AdminChatToAll(string message)
        {
            if (_printToChat)
            {
                Server.PrintToChatAll($"{_adminPrefix} {message}");
            }

            // Also log to normal logging system
            Info($"[ADMIN_CHAT] {message}");
        }

        // Specialized logs
        public void LogMatch(string matchId, string message)
        {
            Info($"[MATCH:{matchId}] {message}");
        }

        public void LogPlayer(ulong steamId, string playerName, string message)
        {
            Info($"[PLAYER:{steamId}:{playerName}] {message}");
        }

        public void LogTeam(int teamId, string teamName, string message)
        {
            Info($"[TEAM:{teamId}:{teamName}] {message}");
        }

        public void LogDatabase(string operation, string message)
        {
            Info($"[DATABASE:{operation}] {message}");
        }
    }
}
