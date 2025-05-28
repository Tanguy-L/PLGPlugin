# PLGPlugin

A comprehensive Counter-Strike 2 server plugin built on CounterStrikeSharp for competitive match management and enhanced gameplay features.

## Features

### 🏆 Match Management

- **Complete Match Flow**: Setup → Knife Round → Side Selection → Live Match → End
- **Team Management**: Database-driven team assignment and side management
- **Demo Recording**: Automatic TV recording with organized file naming
- **Backup System**: Round backup and restoration capabilities
- **Pause/Unpause**: Match pause functionality with team attribution

### 🎮 Player Features

- **Custom Smoke Colors**: 14+ color options for smoke grenades
- **Player Database**: Persistent player data with team associations
- **Ready System**: Team-based ready/unready system
- **Sound Effects**: Custom audio events for kills, bomb events, and special situations

### 🔧 Administration

- **Admin Commands**: Comprehensive command set for match control
- **Team Assignment**: Automatic player team placement based on database
- **Configuration Management**: Hot-reloadable config system
- **Logging**: Detailed logging with multiple severity levels

## Installation

### Prerequisites

- Counter-Strike 2 Dedicated Server
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) framework
- MySQL Database

### Setup Steps

1. **Install CounterStrikeSharp** following their official documentation
2. **Download PLGPlugin** and place it in your CounterStrikeSharp plugins directory:

   ```
   /game/csgo/addons/counterstrikesharp/plugins/PLGPlugin/
   ```

3. **Configure Database** - Create the required MySQL tables (schema not included in codebase)

4. **Configure Plugin** - Edit the configuration file:

   ```json
   {
     "cfg_folder": "PLG/",
     "start_on_match": true,
     "discord_webhook": "",
     "my_sql_config": {
       "host": "localhost",
       "port": 3306,
       "username": "your_username",
       "database": "your_database",
       "password": "your_password"
     }
   }
   ```

5. **Add Required Config Files** in your server's cfg folder:

   - `PLG/warmup.cfg`
   - `PLG/knife.cfg`
   - `PLG/match.cfg`

6. **Install Sound Files** (optional) - Place custom sound files in:

   ```
   /game/csgo/sound/plg_sounds/
   ```

## Commands

### Player Commands

| Command          | Description                         |
| ---------------- | ----------------------------------- |
| `.help`          | Display available commands          |
| `.ready`         | Mark your team as ready             |
| `.unready`       | Mark your team as not ready         |
| `.smoke <color>` | Set your smoke grenade color        |
| `.colors`        | List available smoke colors         |
| `.join`          | Join the team for your current side |
| `.volume <0-1>`  | Adjust sound volume                 |

### Admin Commands

| Command               | Description                               |
| --------------------- | ----------------------------------------- |
| `.match`              | Force start a match (both teams ready)    |
| `.match_on`           | Enable match manager                      |
| `.match_off`          | Disable match manager                     |
| `.match_status`       | Check match manager status                |
| `.warmup`             | Execute warmup config                     |
| `.knife`              | Execute knife round config                |
| `.start`              | Execute live match config                 |
| `.switch`             | Switch sides (knife winner only)          |
| `.stay`               | Stay on current side (knife winner only)  |
| `.pause`              | Pause the match                           |
| `.unpause`            | Unpause the match                         |
| `.load`               | Reload player cache from database         |
| `.list`               | Show all players and their team info      |
| `.set_teams`          | Force all players to their database teams |
| `.lbackups`           | List recent backup files                  |
| `.restore <filename>` | Restore a backup file                     |
| `.map <mapname>`      | Change map                                |
| `.stop_tv`            | Stop TV recording                         |

## Database Schema

The plugin requires the following main database tables:

- `members` - Player information and settings
- `teams` - Team definitions with sides and hostnames
- `team_members` - Player-team associations
- `match_stats_matches` - Match records and statistics
- `match_stats_players` - Individual player match statistics

## Configuration

### Smoke Colors

Available smoke colors include:

- `red`, `green`, `blue`, `blue-night`
- `gold`, `white`, `black`, `turquoise`
- `deep-purple`, `more-pink`, `yellow`, `pink`
- `green-light`, `default`

### Match States

The plugin manages matches through these states:

- `None` - No active match
- `Setup` - Match initialization
- `Knife` - Knife round in progress
- `WaitingForSideChoice` - Waiting for knife winner to choose side
- `Live` - Match is live
- `Paused` - Match is paused
- `Ended` - Match completed

## Architecture

The plugin follows a modular architecture with dependency injection:

- **PlayerManager**: Handles player data and caching
- **TeamManager**: Manages team assignments and sides
- **MatchManager**: Controls match flow and states
- **Database**: MySQL data persistence layer
- **BackupManager**: Round backup file management
- **Sounds**: Custom audio event system

## Development

### Key Classes

- `PLGPlugin`: Main plugin class and entry point
- `MatchManager`: Core match logic and state management
- `PlayerManager`: Player data management with caching
- `Database`: Data access layer with async operations
- `TeamManager`: Team and side management

### Event Handlers

The plugin hooks into various CS2 events:

- Player connect/disconnect
- Round start/end
- Match end
- Player deaths
- Bomb events
- Team changes
