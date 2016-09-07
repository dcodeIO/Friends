Friends
=======
Universal friends plugin for the [Oxide modding framework](https://github.com/OxideMod).

Features
--------
* Written in C# with efficiency in mind
* Configurable to your needs
* Works for every game Oxide supports with Covalence
* Can be extended with game-specific logic
* Exposes an easy to use API to other plugins
* Aims to be API-compatible with [Friends API for Rust](http://oxidemod.org/plugins/friends-api.686/)
* Compatible with BattleLink / Rust:IO 3.X
* Includes configurable friendly fire
* Includes configurable door and turret sharing for Rust

Configuration
-------------
Once installed, the configuration file becomes available at `oxide/config/Friends.json`. To change your settings, simply edit the file and reload the plugin once afterwards.

| Option                  | Default | Description
|-------------------------|---------|-------------
| MaxFriends              | 30      | Limits the number of friends a single player can add
| DisableFriendlyFire     | false   | If `true`, disables friendly fire for friends if supported by the game
| SendOnlineNotification  | true    | If `true`, sends a chat notification to each friend when a player connects
| SendOfflineNotification | true    | If `true`, sends a chat notification to each friend when a player disconnects
| SendAddedNotification   | true    | If `true`, sends a chat notification to the newly added friend
| SendRemovedNotification | true    | If `true`, sends a chat notification to the removed friend

There are additional configuration options for specific games:

#### Rust

| Option                  | Default | Description
|-------------------------|---------|-------------
| ShareCodeLocks          | false   | If `true`, allows a player's friends to use their doors
| ShareAutoTurrets        | false   | If `true`, prevents auto turrets from targeting a player's friends

API
---
The API is pretty much straight forward:

| Method                                                        | Description
|---------------------------------------------------------------|-------------
| HasFriend(playerId:`string`, friendId:`string`):`bool`        | Returns `true` if player's friends list contains friend
| AreMutualFriends(playerId:`string`, friendId:`string`):`bool` | Returns `true` if player's friends list contains friend and friend's friends list contains player
| AddFriend(playerId:`string`, friendId:`string`):`bool`        | Adds friend to player's friends list and returns `true` on success
| RemoveFriend(playerId:`string`, friendId:`string`):`bool`     | Removes friend from player's friends list and returns `true` on success
| GetFriends(playerId:`string`):`IPlayer[]`                     | Returns player's friends as an array of Covalence players

Additionally, the plugin emits its own hooks:

| Hook                                                | Description
|-----------------------------------------------------|-------------
| OnFriendAdded(player:`IPlayer`, friend:`IPlayer`)   | Called when player adds friend to their friends list
| OnFriendRemoved(player:`IPlayer`, friend:`IPlayer`) | Called when player removes friend from their friends list

Contributing
------------
We'll be happy to review your pull request or issue...

* if you'd like to add a your native language.
* if you'd like to contribute game-specific logic for your favorite game.
* if you have found a bug or require a specific feature.

**License:** [MIT](https://opensource.org/licenses/MIT)
