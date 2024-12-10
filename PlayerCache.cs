using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

using System.Collections.Concurrent;

namespace PLGPlugin
{

  public class PlayerManager
  {

    private readonly ConcurrentDictionary<ulong, PlgPlayer> _players = new();
    private readonly Database _database;

    public PlayerManager(Database database)
    {
      _database = database;
    }

    public PlgPlayer? GetPlayer(ulong steamId) =>
            _players.TryGetValue(steamId, out var player) ? player : null;

    public void AddOrUpdatePlayer(PlgPlayer player) =>
        _players.AddOrUpdate(player.SteamID, player, (_, _) => player);

    public void RemovePlayer(ulong steamId) =>
        _players.TryRemove(steamId, out _);

    public void ClearCache() =>
      _players.Clear();


    public async Task AddPlgPlayer(CCSPlayerController playerController)
    {

      if (playerController != null)
      {
        var playerPLG = new PlgPlayer(playerController);
        var steamId = playerController.SteamID;
        var playerInfosDB = await _database.GetPlayerById(steamId);

        if (playerInfosDB != null)
        {
          playerPLG.Side = playerInfosDB.Side;
          playerPLG.TeamName = playerInfosDB.TeamName;
          playerPLG.SmokeColor = playerInfosDB.SmokeColor;
          playerPLG.DiscordId = playerInfosDB.DiscordId;
          playerPLG.TeamChannelId = playerInfosDB.TeamChannelId;
          if (playerInfosDB.Weight != null)
          {
            playerPLG.Weight = playerInfosDB.Weight.ToString();
          }
        }
        AddOrUpdatePlayer(playerPLG);
      }
    }

    public async Task LoadCache()
    {
      var allPlayers = Utilities.GetPlayers();
      var tasks = allPlayers.Select(AddPlgPlayer);
      await Task.WhenAll(tasks);
    }
  }
}