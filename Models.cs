using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CounterStrikeSharp.API.Core;

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
        return controller.IsValid == true
            && controller.PlayerPawn?.IsValid == true
            && controller.Connected == PlayerConnectedState.PlayerConnected;
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

public class Color(int red, int green, int blue)
{
    public float Red { get; set; } = red;
    public float Blue { get; set; } = blue;
    public float Green { get; set; } = green;
}

public static class SmokeColorPalette
{
    public static readonly Dictionary<string, Color> Colors = new()
    {
        { "red", new Color(255, 0, 0) },
        { "green", new Color(0, 255, 0) },
        { "blue", new Color(0, 0, 255) },
        { "blue-night", new Color(11, 43, 64) },
        { "gold", new Color(255, 179, 13) },
        { "white", new Color(255, 255, 255) },
        { "black", new Color(32, 32, 34) },
        { "turquoise", new Color(0, 187, 201) },
        { "deep-purple", new Color(64, 0, 54) },
        { "more-pink", new Color(255, 129, 208) },
        { "yellow", new Color(255, 255, 0) },
        { "pink", new Color(255, 20, 147) },
        { "default", new Color(0, 0, 0) },
        { "green-light", new Color(137, 217, 157) },
    };

    public static IEnumerable<string> GetAllColorKeys()
    {
        return Colors.Keys;
    }

    public static Color GetColorByKey(string key)
    {
        if (Colors.TryGetValue(key, out var color))
        {
            return color;
        }
        else
        {
            throw new KeyNotFoundException($"Color key '{key}' not found in SmokeColorPalette.");
        }
    }
}
#endregion
