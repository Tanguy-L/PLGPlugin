using CounterStrikeSharp.API.Core;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PLGPlugin;

#region Player Models
public class PlgPlayer
{
    public ulong SteamID { get; }
    public string? Side { get; set; }
    public string PlayerName { get; }
    public string? DiscordId { get; set; }
    public bool IsLoggedIn { get; set; }
    public string? Weight { get; set; }
    public string? SmokeColor { get; set; }
    public string? TeamName { get; set; }
    public string? TeamChannelId { get; set; }
    public bool IsReady { get; set; }

    public PlgPlayer(CCSPlayerController controller)
    {
        SteamID = controller.SteamID;
        PlayerName = controller.PlayerName;
        IsReady = false;
    }

    public bool isValid(CCSPlayerController controller)
    {
        return controller.IsValid == true &&
        controller.PlayerPawn?.IsValid == true &&
        controller.Connected == PlayerConnectedState.PlayerConnected;
    }

    public bool IsPlayer(CCSPlayerController controller)
    {
        return !controller.IsBot && !controller.IsHLTV;
    }
}

public class PlayerFromDB
{
    [Key]
    [Column("discord_id")]
    public string? DiscordId { get; set; }

    [Column("discord_name")]
    public string? DiscordName { get; set; }

    [Column("steam_id")]
    public string? SteamId { get; set; }

    [Column("is_logged_in")]
    public bool? IsLoggedIn { get; set; }

    [Column("weight")]
    public int? Weight { get; set; }

    [Column("smoke_color")]
    public string? SmokeColor { get; set; }

    [Column("team_name")]
    public string? TeamName { get; set; }

    [Column("team_channel_id")]
    public string? TeamChannelId { get; set; }

    [Key]
    [Column("team_side")]
    public string? Side { get; set; }
}
#endregion
