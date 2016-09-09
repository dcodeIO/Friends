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
* Includes configurable friends broadcast and private chat

Usage
-----
As a player, this is how you use the plugin:

| Command                      | Description
|------------------------------|-------------
| /friends                     | Displays your friends list and essential usage instructions
| /addfriend *NAME...*         | Adds a player to your friends
| /removefriend *NAME...*      | Removes a player from your friends
| /fm *MESSAGE...*             | Broadcasts a message to all of your friends
| /pm "*NAME...*" *MESSAGE...* | Sends a private message to the specified player

You can also use the respective unique player id (i.e. Steam ID) instead of a player's name in case that there are multiple
players using the same name.

Also note that **/fm** and **/pm** may not be available if the server owner decided so, and if available, be aware that these
messages can trivially be logged on the server even if there is no logging mechanism by default.

Configuration
-------------
Once installed, the configuration file becomes available at `oxide/config/Friends.json`. To change your settings, simply edit the file and reload the plugin once afterwards.

| Option                         | Default | Description
|--------------------------------|---------|-------------
| MaxFriends                     | 30      | Limits the number of friends a single player can add
| DisableFriendlyFire            | false   | If `true`, disables friendly fire for friends if supported by the game
| EnableFriendChat               | false   | If `true`, enables friend chat through the **/fm** command.
| LimitFriendChatToMutualFriends | true    | If `true`, limits friend chat to mutual friends.
| EnablePrivateChat              | false   | If `true`, enables private messages through the **/pm** command.
| SendOnlineNotification         | true    | If `true`, sends a chat notification to each friend when a player connects
| SendOfflineNotification        | true    | If `true`, sends a chat notification to each friend when a player disconnects
| SendAddedNotification          | true    | If `true`, sends a chat notification to the newly added friend
| SendRemovedNotification        | false   | If `true`, sends a chat notification to the removed friend

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
| GetMaxFriends():`int`                                         | Gets the maximum number of friends allowed per player.
| GetPlayerName(*playerId*:`object`):`string`                   | Gets *player*'s current or remembered name, by id.
| HasFriend(*playerId*:`object`, *friendId*:`object`):`bool`    | Tests if *player* added *friend* to their friends list, by id.
| AreFriends(*playerId*:`object`, *friendId*:`object`):`bool`   | Tests if *player* and *friend* are mutual friends, by id.
| AddFriend(*playerId*:`object`, *friendId*:`object`):`bool`    | Adds *friend* to *player*'s friends list, by id.
| RemoveFriend(*playerId*:`object`, *friendId*:`object`):`bool` | Removes *friend* from *player*'s friends list, by id.
| GetFriends(*playerId*:`object`):`object`                      | Gets an array of *player*'s friends, by id.
| GetFriendsReverse(*playerId*:`object`):`object`               | Gets an array of players who have added *player* to their friends list, by id.

**Note** that all methods take arbitrary parameter types (i.e. `string`, `ulong` or `int`), which makes them independent
of what the game being modded uses to represent player ids. **GetFriends** and **GetFriendsReverse** in particular return an
array of the specified parameter's type (i.e. `ulong[]` if the parameter was `ulong`).

Other methods declared within the source file exist for compatibility purposes only and should not be used in new projects.

Additionally, the plugin emits its own hooks:

| Hook                                                      | Description
|-----------------------------------------------------------|-------------
| OnFriendAdded(*playerId*:`string`, *friendId*:`string`)   | Called when *player* adds *friend* to their friends list, by id.
| OnFriendRemoved(*playerId*:`string`, *friendId*:`string`) | Called when *player* removes *friend* from their friends list, by id.

#### Example

```cs
[PluginReference]
Plugin Friends;

...
{
    var hasFriend = Friends?.Call<bool>("HasFriend", playerId, friendId) ?? false;
    ...
}

OnFriendAdded(IPlayer player, IPlayer friend)
{
    ...
}
```

Contributing
------------
We'll be happy to review your pull request or issue...

* if you'd like to add support for your native language.
* if you'd like to contribute game-specific logic for your favorite game.
* if you have found a bug or require a specific feature.

**License:** [MIT](https://opensource.org/licenses/MIT)
