using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

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

        public List<PlgPlayer> getAllPlayers()
        {
            return _players.Values.ToList();
        }

        public PlgPlayer? GetPlayer(ulong steamId) =>
            _players.TryGetValue(steamId, out var player) ? player : null;

        public void AddOrUpdatePlayer(PlgPlayer player) =>
            _players.AddOrUpdate(player.SteamID, player, (_, _) => player);

        public void RemovePlayer(ulong steamId) => _players.TryRemove(steamId, out _);

        public void ClearCache() => _players.Clear();

        public void UpdatePlayerWithData(
            CCSPlayerController playerController,
            PlayerFromDB playerDB
        )
        {
            if (playerController == null)
                return;

            var playerPLG = new PlgPlayer(playerController);
            if (playerDB != null)
            {
                playerPLG.Side = playerDB.Side;
                playerPLG.TeamName = playerDB.TeamName;
                playerPLG.SmokeColor = playerDB.SmokeColor;
                playerPLG.DiscordId = playerDB.DiscordId;
                playerPLG.TeamChannelId = playerDB.TeamChannelId;
                playerPLG.MemberId = playerDB.MemberId;
                if (playerDB.Weight != null)
                {
                    playerPLG.Weight = playerDB.Weight.ToString();
                }
            }
            AddOrUpdatePlayer(playerPLG);
        }

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
                    playerPLG.MemberId = playerInfosDB.MemberId;
                    if (playerInfosDB.Weight != null)
                    {
                        playerPLG.Weight = playerInfosDB.Weight.ToString();
                    }
                }
                else
                {
                    await _database.CreatePlayerInDB(playerPLG);
                }
                AddOrUpdatePlayer(playerPLG);
            }
        }

        public async Task LoadCache()
        {
            var allPlayers = Utilities.GetPlayers();
            foreach (var player in allPlayers)
            {
                await AddPlgPlayer(player);
            }
        }
    }
}
