using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Core.Attributes.Registration;
namespace PLGPlugin;

public sealed partial class PLGPlugin
{
  [ConsoleCommand("css_load", "Reload the player current cache")]
  public void LoadPlayerCache(CCSPlayerController? player, CommandInfo? command)
  {

    if (_playerManager != null)
    {
      Task.Run(async () =>
      {
        await _playerManager.LoadCache();
      });
    }
  }
}
