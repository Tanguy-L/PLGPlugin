using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Events;

namespace PLGPlugin;


public sealed class MySQLConfig
{
    [JsonPropertyName("host")]
    public string HostDB { get; set; } = "localhost";

    [JsonPropertyName("port")]
    public int Port { get; set; } = 3306;

    [JsonPropertyName("username")]
    public string Username { get; set; } = "";

    [JsonPropertyName("database")]
    public string Database { get; set; } = "";

    [JsonPropertyName("password")]
    public string Password { get; set; } = "";
}

public class PlgConfig : BasePluginConfig
{
    [JsonPropertyName("cfg_folder")]
    public string CfgFolder { get; set; } = "PLG/";

    [JsonPropertyName("start_on_match")]
    public bool StartOnMatch { get; set; } = true;

    [JsonPropertyName("discord_webhook")]
    public string DiscordWebhook { get; set; } = "";

    [JsonPropertyName("my_sql_config")]
    public MySQLConfig MySQLConfig { get; set; } = new MySQLConfig();
}

public sealed partial class PLGPlugin : BasePlugin, IPluginConfig<PlgConfig>
{
    public override string ModuleName => "PLGPlugin";
    public override string ModuleDescription => "PLGPlugin";
    public override string ModuleAuthor => "PLGPlugin";
    public override string ModuleVersion => "1.0.0";

    internal static PLGPlugin Instance { get; private set; } = new();
    public PlgConfig Config { get; set; } = new PlgConfig();
    public PlayerManager? _playerManager;
    public Database? _database;
    private BackupManager? _backup;
    public MatchManager? _matchManager;
    private Sounds? _sounds;
    private TeamManager? _teams;
    public new LoggingService? Logger;

    public void OnConfigParsed(PlgConfig config)
    {
        Config = config;
    }

    public override void Load(bool hotReload)
    {
        // ---------------
        // LOAD LOGGER 
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        var coreLogger = loggerFactory.CreateLogger<PLGPlugin>();

        LoggingService.Initialize(coreLogger,
            printToConsole: false,
            printToChat: false,
            printToServer: true);

        Logger = LoggingService.Instance;

        if (Logger == null)
        {
            Console.WriteLine("Logger is null");
        }

        // Loading db
        _database = new(Config.MySQLConfig);

        // loading basics
        if (_database != null)
        {
            _playerManager = new(_database);
            _teams = new();
        }
        _backup = new();
        _sounds = new();


        // ---------------
        // LOAD EVERYTHING
        InitMatchManager();

        // ---------------
        // CHECK INIT
        if (Logger != null)
        {
            Logger.Info("-------- PLG ---------");
            Logger.Info(_database != null ? "Database connected" : "No database");
            Logger.Info(_playerManager != null ? "Player manager created" : "No player manager");
            Logger.Info(_backup != null ? "Backup manager created" : "No backup manager");
            Logger.Info(_teams != null ? "Team manager created" : "No team manager");
            Logger.Info("-------- PLG ---------");
        }

        InitializeEvents();

        // Simple command - Event bind
        Dictionary<string, Action<CCSPlayerController?, CommandInfo?>> commandActions = new()
        {
            { ".load", LoadPlayerCache },
            { ".match_status", GetMatchManagerStatus },
            { ".list", ListPlayers },
            { ".stop_tv", OnStopRecordTv },
            { ".ready", OnReady },
            { ".unready", OnUnready },
            { ".colors", PrintColors },
            { ".match", OnStartPLGMatch},
            { ".warmup", Warmup },
            { ".knife", StartKnife },
            { ".start", StartLive },
            { ".switch", Switch },
            { ".stay", OnStay },
            { ".help", OnHelpCommand },
            { ".pause", OnPauseCommand },
            { ".unpause", OnUnpauseCommand },
            { ".set_teams", OnSetTeams },
            { ".match_off", MatchManagerOff },
            { ".match_on", MatchManagerOn },
            { ".lbackups", OnGetBackups },
            { ".test", OnTestCommand },
        };


        // Chat event
        // it need for using commands with arguments like .smoke red
        RegisterEventHandler<EventPlayerChat>(
            (@event, info) =>
            {
                int currentVersion = Api.GetVersion();
                int index = @event.Userid + 1;
                CCSPlayerController? playerController = Utilities.GetPlayerFromIndex(index);

                if (playerController == null || _database == null || _playerManager == null)
                {
                    return HookResult.Continue;
                }

                var steamId = playerController.SteamID;
                var originalMessage = @event.Text.Trim();
                var message = @event.Text.Trim().ToLower();
                var parts = message.Split(' ');

                if (commandActions.ContainsKey(message))
                {
                    commandActions[message](playerController, null);
                }

                if (message.StartsWith(".map"))
                {
                    var messageCommandArg =
                        parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : string.Empty;

                    HandleMapChangeCommand(playerController, messageCommandArg);
                    return HookResult.Continue;
                }

                if (message.StartsWith(".restore"))
                {
                    var messageCommandArg =
                        parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : string.Empty;

                    HandleRestore(playerController, messageCommandArg);
                    return HookResult.Continue;
                }

                if (message.StartsWith(".volume"))
                {
                    var messageCommandArg =
                        parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : string.Empty;

                    playerController?.ExecuteClientCommand($"snd_toolvolume {messageCommandArg}");
                }

                bool predicate = message.StartsWith(".smoke");

                if (predicate)
                {
                    string command = ".smoke";
                    // color of the smoke
                    string commandArg = message[command.Length..].Trim();

                    Server.NextFrame(async () =>
                    {
                        if (playerController != null)
                        {
                            await HandleUpdateSmoke(playerController, commandArg);
                        }
                    });
                }
                return HookResult.Continue;
            }
        );
        base.Load(hotReload);
    }
}
