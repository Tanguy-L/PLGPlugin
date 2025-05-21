using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using CounterStrikeSharp.API.Modules.Cvars;
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
            printToConsole: true,
            printToChat: true,
            printToServer: true);

        Logger = LoggingService.Instance;

        Logger.Info($"PLGPlugin v{ModuleVersion} is loading.");

        if (Logger == null)
        {
            Console.WriteLine("Logger is null");
        }

        // Loading db
        _database = new(Config.MySQLConfig);

        // loading basics
        if (_database != null)
        {
            Logger?.Info("Database connected");
            _playerManager = new(_database);
            _teams = new();
        }
        _backup = new();
        _sounds = new();


        // ---------------
        // LOAD EVERYTHING
        if (_database != null && _playerManager != null && _backup != null && _teams != null)
        {
            try
            {
                _matchManager = new MatchManager(_database, _playerManager, Config, _backup, _teams);
                Logger?.Info("Match manager initialized successfully");

                if (Config.StartOnMatch && _matchManager != null)
                {
                    var hostnameValue = ConVar.Find("hostname")?.StringValue;
                    if (string.IsNullOrEmpty(hostnameValue))
                    {
                        Logger?.Warning("Cannot initialize match: hostname is null or empty");
                        return;
                    }

                    // Handle async database operations properly
                    Server.NextFrame(async () =>
                    {
                        try
                        {
                            var teams = await _database.GetTeamsByHostname(hostnameValue);

                            // Validate teams data
                            if (teams == null || teams.Count < 2)
                            {
                                Logger?.Error($"Failed to load teams for hostname: {hostnameValue}. Not enough teams returned.");
                                return;
                            }

                            // Schedule UI updates on the main thread
                            Server.NextFrame(() =>
                            {
                                try
                                {
                                    // Reinitialize teams manager with loaded data
                                    _teams = new TeamManager();
                                    _teams.AddTeam(teams[0]);
                                    _teams.AddTeam(teams[1]);

                                    // Initialize the match
                                    _matchManager.InitSetupMatch(hostnameValue);
                                    Logger?.Info($"Match initialized successfully for {hostnameValue}");
                                }
                                catch (Exception ex)
                                {
                                    Logger?.Error($"Error during match setup: {ex.Message}");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Logger?.Error($"Database error when fetching teams: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logger?.Error($"Failed to initialize match manager: {ex.Message}");
            }
        }
        else
        {
            // More descriptive error that indicates exactly what's missing
            Logger?.Error($"Cannot initialize match manager. Missing dependencies: " +
                         $"{(_database == null ? "database " : "")}" +
                         $"{(_playerManager == null ? "playerManager " : "")}" +
                         $"{(_backup == null ? "backup " : "")}" +
                         $"{(_teams == null ? "teams" : "")}");
        }


        // if (_database != null && _playerManager != null && _backup != null && _teams != null)
        // {
        //     _matchManager = new MatchManager(_database, _playerManager, Config, _backup, _teams);
        //     Logger?.Info("Load everything");
        //     if (Config.StartOnMatch && _matchManager != null)
        //     {
        //         var hostnameValue = ConVar.Find("hostname")?.StringValue;
        //         if (hostnameValue != null)
        //         {
        //             Server.NextFrame(async () =>
        //             {
        //
        //                 var teams = await _database.GetTeamsByHostname(hostnameValue);
        //                 Server.NextFrame(() =>
        //                 {
        //                     Logger?.Info($"teams: {teams[0].Name} {teams[1].Name}");
        //
        //                     _teams = new TeamManager();
        //                     _teams.AddTeam(teams[0]);
        //                     _teams.AddTeam(teams[1]);
        //                     _matchManager.InitSetupMatch(hostnameValue);
        //                 });
        //
        //                 Logger?.Info("Match initialized");
        //             });
        //         }
        //     }
        // }
        // else
        // {
        //     Logger?.Error("Miss _database or _playerManager or _backup or _teams");
        // }

        // ---------------
        // CHECK INIT
        if (Logger != null)
        {
            Logger.Info(_database != null ? "Database connected" : "No database");
            Logger.Info(_playerManager != null ? "Player manager created" : "No player manager");
            Logger.Info(_backup != null ? "Backup manager created" : "No backup manager");
            Logger.Info(_teams != null ? "Team manager created" : "No team manager");
        }

        InitializeEvents();

        // Simple command - Event bind
        Dictionary<string, Action<CCSPlayerController?, CommandInfo?>> commandActions = new()
        {
            { ".load", LoadPlayerCache },
            { ".stop_tv", OnStopRecordTv },
            { ".ready", OnReady },
            { ".unready", OnUnready },
            { ".list", ListPlayers },
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
            { ".no_match", OnNoMatch },
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
