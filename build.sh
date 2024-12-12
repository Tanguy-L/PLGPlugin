#!/bin/bash
SERVER_PLUGIN_PATH="${HOME}/steamcmd/cs2server/game/csgo/addons/counterstrikesharp/plugins"
PLUGIN_NAME="PLGPlugin"

rm -rf "$PLUGIN_NAME"

dotnet publish -c Release

rm -rf "$SERVER_PLUGIN_PATH/$PLUGIN_NAME"

cp -r "$PLUGIN_NAME/" "$SERVER_PLUGIN_PATH/"

echo "$PLUGIN_NAME cleared and copied successfully"
