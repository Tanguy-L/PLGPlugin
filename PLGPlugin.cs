using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Utils;

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
    public string CfgFolder { get; set; } = "MatchPlg/";

    [JsonPropertyName("my_sql_config")]
    public MySQLConfig MySQLConfig { get; set; } = new MySQLConfig();
}

public sealed partial class PLGPlugin : BasePlugin, IPluginConfig<PlgConfig>
{
    public override string ModuleName => "PLGPlugin";
    public override string ModuleDescription => "PLGPlugin";
    public override string ModuleAuthor => "PLGPlugin";
    public override string ModuleVersion => "1.0.0";
    public PlgConfig Config { get; set; } = new PlgConfig();
    public PlayerManager? _playerManager;
    public Database? _database;

    public void OnConfigParsed(PlgConfig config)
    {
        Config = config;
        _database = new(config.MySQLConfig);

        if (_database != null)
        {
            _playerManager = new(_database);
        }
    }

    public override void Load(bool hotReload)
    {
        InitializeEvents();

        // Simple command - Event bind
        Dictionary<string, Action<CCSPlayerController?, CommandInfo?>> commandActions = new()
        {
            { ".load", LoadPlayerCache },
            { ".list", ListPlayers },
            { ".colors", PrintColors },
            { ".warmup", Warmup },
            { ".knife", StartKnife },
            { ".start", StartLive },
            { ".switch", Switch },
            { ".help", OnHelpCommand },
            { ".pause", OnPauseCommand },
            { ".unpause", OnUnpauseCommand },
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
                        await HandleUpdateSmoke(playerController, commandArg);
                    });
                }
                return HookResult.Continue;
            }
        );
        base.Load(hotReload);
    }
}
