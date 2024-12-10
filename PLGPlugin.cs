using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using System.Text.Json.Serialization;
namespace PLGPlugin;

public sealed class MySQLConfig
{

  [JsonPropertyName("host")] public string HostDB { get; set; } = "localhost";
  [JsonPropertyName("port")] public int Port { get; set; } = 3306;
  [JsonPropertyName("username")] public string Username { get; set; } = "";
  [JsonPropertyName("database")] public string Database { get; set; } = "";
  [JsonPropertyName("password")] public string Password { get; set; } = "";
}

public class PlgConfig : BasePluginConfig
{
  [JsonPropertyName("cfg_folder")] public string CfgFolder { get; set; } = "MatchPlg/";
  [JsonPropertyName("my_sql_config")] public MySQLConfig MySQLConfig { get; set; } = new MySQLConfig();
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
    _playerManager = new(_database);
  }

  public override void Load(bool hotReload)
  {


    Dictionary<string, Action<CCSPlayerController?, CommandInfo?>> commandActions = new()
    {
      { ".load", LoadPlayerCache }
    };


    base.Load(hotReload);
  }
}
