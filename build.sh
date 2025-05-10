#!/bin/bash
SERVER_PLUGIN_PATH="${HOME}/steamcmd/cs2server/game/csgo/addons/counterstrikesharp/plugins"
PLUGIN_NAME="PLGPlugin"

if [ -d "$PLUGIN_NAME" ]; then
	rm -rf "$PLUGIN_NAME"
	echo "Removed local $PLUGIN_NAME directory"
else
	echo "Local $PLUGIN_NAME directory doesn't exist, skipping removal"
fi

dotnet publish -c Release

if [ -d "$SERVER_PLUGIN_PATH/$PLUGIN_NAME" ]; then
	rm -rf "$SERVER_PLUGIN_PATH/$PLUGIN_NAME"
	echo "Removed $PLUGIN_NAME from server plugins directory"
else
	echo "Server plugin directory $SERVER_PLUGIN_PATH/$PLUGIN_NAME doesn't exist, skipping removal"
fi

cp -r "$PLUGIN_NAME/" "$SERVER_PLUGIN_PATH/"

echo "$PLUGIN_NAME cleared and copied successfully"
