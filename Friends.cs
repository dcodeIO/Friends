#region License
/*
 Copyright (c) 2016 dcode / BattleLink.io

 Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated
 documentation files (the "Software"), to deal in the Software without restriction, including without limitation
 the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
 and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

 The above copyright notice and this permission notice shall be included in all copies or substantial portions
 of the Software.

 THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED
 TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
 THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
 CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 DEALINGS IN THE SOFTWARE.

 See: https://github.com/BattleLink/Friends for details
*/
#endregion
#define RUST
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Friends", "dcode", "3.0.0", ResourceId = 2120)]
    [Description("Universal friends plugin.")]
    public class Friends : CovalencePlugin
    {
        #region Config

        class ConfigData
        {
            // DO NOT EDIT! These are the defaults. Edit oxide/config/Friends.json instead!

            public int MaxFriends = 30;
            public bool DisableFriendlyFire = false;

            public bool SendOnlineNotification = true;
            public bool SendOfflineNotification = true;
            public bool SendAddedNotification = true;
            public bool SendRemovedNotification = true;
#if RUST
            public RustConfigData Rust = new RustConfigData();
#endif
        }

#if RUST
        class RustConfigData
        {
            public bool ShareCodeLocks = false;
            public bool ShareAutoTurrets = false;
        }
#endif

        ConfigData configData;

        protected override void LoadDefaultConfig() => Config.WriteObject(configData = new ConfigData(), true);

        #endregion

        #region Language

        void registerMessages()
        {
            // English [en]
            lang.RegisterMessages(new Dictionary<string, string> {

                // Command replies
                { "PlayerNotFound", "There is no player matching that name." },
                { "NotOnFriendlist", "You don't have a friend matching that name." },
                { "FriendAdded", "{0} is now one of your friends." },
                { "FriendRemoved", "{0} is no longer one of your friends." },
                { "AlreadyAFriend", "{0} is already one of your friends." },
                { "CantAddSelf", "You cannot add yourself to your friends." },
                { "NoFriends", "You haven't added any friends, yet." },
                { "List", "You have {0} friends:" },
                { "ListOnline", "[ONLINE]" },
                { "FriendlistFull", "You have already reached the maximum number of friends." },

                // Chat notifications
                { "FriendAddedNotification", "{0} added you as a friend." },
                { "FriendRemovedNotification", "{0} removed you as a friend." },
                { "FriendOnlineNotification", "{0} is now online!" },
                { "FriendOfflineNotification", "{0} is now offline." },
            
                // Usage text
                { "UsageAdd", "Use /addfriend NAME... to add a friend" },
                { "UsageRemove", "Use /removefriend NAME... to remove one" },
                { "HelpText", "Type /friends to manage your friends" }

            }, this, "en");

            // Deutsch [de]
            lang.RegisterMessages(new Dictionary<string, string> {

                // Command replies
                { "PlayerNotFound", "Es gibt keinen Spieler unter diesem Namen." },
                { "NotOnFriendlist", "Auf deiner Freundeliste befindet sich kein Spieler mit diesem Namen." },
                { "FriendAdded", "{0} ist nun einer deiner Freunde." },
                { "FriendRemoved", "{0} ist nun nicht mehr dein Freund." },
                { "AlreadyAFriend", "{0} ist bereits dein Freund." },
                { "CantAddSelf", "Du kannst dich nicht selbst als Freund hinzufügen." },
                { "NoFriends", "Du hast noch keine Freunde hinzugefügt." },
                { "List", "Du hast {0} von maximal {1} Freunden:" },
                { "ListOnline", "[ONLINE]" },
                { "FriendlistFull", "Du hast bereits die maximale Anzahl an Freunden erreicht." },

                // Chat notifications
                { "FriendAddedNotification", "{0} hat dich als Freund hinzugefügt." },
                { "FriendRemovedNotification", "{0} hat dich als Freund entfernt." },
                { "FriendOnlineNotification", "{0} ist jetzt online!" },
                { "FriendOfflineNotification", "{0} ist jetzt offline." },
            
                // Usage text
                { "UsageAdd", "Verwende /addfriend NAME... um Freunde hinzuzufügen" },
                { "UsageRemove", "Verwende /removefriend NAME... um Freunde zu entfernen" },
                { "HelpText", "Schreibe /friends um deine Freunde zu verwalten" }

            }, this, "de");

        }

        string _(string key, string translateFor) => lang.GetMessage(key, this, translateFor);

        #endregion

        #region Persistence

        class PlayerData { public string Name; public HashSet<string> Friends; }

        Dictionary<string, PlayerData> playerData;

        Dictionary<string, HashSet<string>> reverseData;

        void loadData() => playerData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, PlayerData>>(Name);

        void loadConfig() => configData = Config.ReadObject<ConfigData>();

        void saveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, playerData);

        #endregion

        #region Hooks

        // Object references to boolean values for use with short if statements.
        readonly object @true = true;
        readonly object @false = false;

        void Loaded()
        {
            loadConfig();
            loadData();
            registerMessages();
            reverseData = new Dictionary<string, HashSet<string>>();
            if (playerData == null)
                playerData = new Dictionary<string, PlayerData>();
            else
                // Rebuild reverse lookup
                foreach (var kv in playerData)
                    foreach (var friendId in kv.Value.Friends)
                    {
                        HashSet<string> reverseFriendData;
                        if (reverseData.TryGetValue(friendId, out reverseFriendData))
                            reverseFriendData.Add(kv.Key);
                        else
                            reverseData.Add(friendId, new HashSet<string>() { kv.Key });
                    }
        }

        void OnUserConnected(IPlayer player)
        {
            // Update the player's remembered name if necessary
            PlayerData data;
            if (playerData.TryGetValue(player.Id, out data))
                if (player.Name != data.Name)
                {
                    data.Name = player.Name;
                    saveData();
                }

            // Send online notifications if enabled
            if (configData.SendOnlineNotification && data != null)
                foreach (var friendId in data.Friends)
                {
                    var friend = covalence.Players.GetPlayer(friendId);
                    if (friend != null && friend.IsConnected)
                        friend.Message(_("FriendOnlineNotification", friend.Id), player.Name);
                }
        }

        void OnUserDisconnected(IPlayer player)
        {
            // Send offline notifications if enabled
            PlayerData data;
            if (configData.SendOnlineNotification && playerData.TryGetValue(player.Id, out data))
                foreach (var friendId in data.Friends)
                {
                    var friend = covalence.Players.GetPlayer(friendId);
                    if (friend != null && friend.IsConnected)
                        friend.Message(_("FriendOfflineNotification", friend.Id), player.Name);
                }
        }

        // Reusable collections meant to reduce allocations / garbage
        readonly List<string> onlineList = new List<string>(30);
        readonly List<string> offlineList = new List<string>(30);

        [Command("friends")]
        void cmdFriends(IPlayer player, string command, string[] args)
        {
            PlayerData data;
            if (playerData.TryGetValue(player.Id, out data) && data.Friends.Count > 0)
            {
                player.Reply(_("List", player.Id));
                foreach (var friendId in data.Friends)
                {
                    // Sort friends by online status and name (must be mutual friends to show online status)
                    var friend = covalence.Players.GetPlayer(friendId);
                    if (friend != null)
                    {
                        if (friend.IsConnected && HasFriend(friend.Id, player.Id))
                            onlineList.Add(friend.Name);
                        else
                            offlineList.Add(friend.Name);
                    }
                    else
                    {
                        PlayerData friendData;
                        if (playerData.TryGetValue(friendId, out friendData))
                            offlineList.Add(friendData.Name);
                        else
                            offlineList.Add("#" + friendId);
                    }
                }
                onlineList.Sort((a, b) => string.Compare(a, b, StringComparison.InvariantCultureIgnoreCase));
                var onlineText = _("ListOnline", player.Id);
                foreach (var friendName in onlineList)
                    player.Message(onlineText + " " + friendName);
                onlineList.Clear();
                offlineList.Sort((a, b) => string.Compare(a, b, StringComparison.InvariantCultureIgnoreCase));
                foreach (var friendName in offlineList)
                    player.Message(friendName);
                offlineList.Clear();
            }
            else
                player.Reply(_("NoFriends", player.Id));
            player.Message(_("UsageAdd", player.Id));
            player.Message(_("UsageRemove", player.Id));
        }

        [Command("addfriend")]
        void cmdAddFriend(IPlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                player.Reply(_("UsageAdd", player.Id));
                return;
            }
            var name = string.Join(" ", args);
            var friend = covalence.Players.FindPlayer(name);
            if (friend == null)
            {
                player.Reply(_("NoSuchPlayer", player.Id));
                return;
            }
            if (friend == player)
            {
                player.Reply(_("CantAddSelf", player.Id));
                return;
            }
            PlayerData data;
            if (configData.MaxFriends < 1 || playerData.TryGetValue(player.Id, out data) && data.Friends.Count >= configData.MaxFriends)
                player.Reply(_("FriendlistFull", player.Id));
            else if (AddFriend(player.Id, friend.Id))
                player.Reply(_("FriendAdded", player.Id), friend.Name);
            else
                player.Reply(_("AlreadyAFriend", player.Id), friend.Name);
        }

        [Command("removefriend", "deletefriend")]
        void cmdRemoveFriend(IPlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                player.Reply(_("UsageRemove", player.Id));
                return;
            }
            var name = string.Join(" ", args);
            var friend = covalence.Players.FindPlayer(name);
            if (friend == null)
                player.Reply(_("NoSuchPlayer", player.Id));
            else if (RemoveFriend(player.Id, friend.Id))
                player.Reply(_("FriendRemoved", player.Id), friend.Name);
            else
                player.Reply(_("NoSuchFriend", player.Id));
        }

        #endregion

        #region API

        // Internal method to determine whether this plugin is compatible with BattleLink.
        // Compatibility with BattleLink basically means that friendships are non-mutual
        // by default because if they were not, this would lead to abuse of live locations,
        // and that API methods always have a generic implementation.
        bool IsBattleLinkCompatible() => true;

        // Returns the maximum number of friends allowed per player.
        int MaxFriends() => configData.MaxFriends;

        // Gets a player's name, by id.
        public string GetPlayerName(string playerId)
        {
            if (ReferenceEquals(playerId, null))
                throw new ArgumentNullException("playerId");
            var iplayer = covalence.Players.GetPlayer(playerId);
            if (iplayer == null)
            {
                PlayerData data;
                if (playerData.TryGetValue(playerId, out data))
                    return data.Name;
            }
            else
                return iplayer.Name;
            return "#" + playerId;
        }

        // Gets a player's name, by id, generic.
        public string GetPlayerName<T>(T playerId) where T : struct => GetPlayerName(playerId.ToString());

        // Tests if player added friend to their friends list, by id.
        bool HasFriend(string playerId, string friendId)
        {
            if (ReferenceEquals(playerId, null))
                throw new ArgumentNullException("playerId");
            if (ReferenceEquals(friendId, null))
                throw new ArgumentNullException("friendId");
            PlayerData data;
            return playerData.TryGetValue(playerId, out data) && data.Friends.Contains(friendId);
        }

        // Tests if player added friend to their friends list, by id, generic.
        bool HasFriend<T>(T playerId, T friendId) where T : struct => HasFriend(playerId.ToString(), friendId.ToString());

        // Tests if player and friend are mutual friends, by id.
        bool AreFriends(string playerId, string friendId) => HasFriend(playerId, friendId) && HasFriend(friendId, playerId);

        // Tests if player and friend are mutual friends, by id, generic.
        bool AreFriends<T>(T playerId, T friendId) where T : struct {
            var playerIdStr = playerId.ToString();
            var friendIdStr = friendId.ToString();
            return HasFriend(playerIdStr, friendIdStr) && HasFriend(friendIdStr, playerIdStr);
        }

        // Adds friend to player's friends list, by id.
        bool AddFriend(string playerId, string friendId)
        {
            if (ReferenceEquals(playerId, null))
                throw new ArgumentNullException("playerId");
            if (ReferenceEquals(friendId, null))
                throw new ArgumentNullException("friendId");
            var player = covalence.Players.GetPlayer(playerId);
            if (player == null)
                return false;
            var friend = covalence.Players.GetPlayer(friendId);
            if (friend == null)
                return false;
            PlayerData data;
            if (playerData.TryGetValue(player.Id, out data))
            {
                if (data.Friends.Count >= configData.MaxFriends)
                    return false;
                data.Friends.Add(friend.Id);
            }
            else
                data = playerData[player.Id] = new PlayerData() { Name = player.Name, Friends = new HashSet<string>() { friend.Id } };
            if (!playerData.TryGetValue(friend.Id, out data)) // also add a blank reverse entry remembering the friend's name
                playerData[friend.Id] = new PlayerData() { Name = friend.Name, Friends = new HashSet<string>() };
            saveData();
            HashSet<string> reverseFriendData;
            if (reverseData.TryGetValue(friend.Id, out reverseFriendData))
                reverseFriendData.Add(player.Id);
            else
                reverseData.Add(friend.Id, new HashSet<string>() { player.Id});
            if (configData.SendAddedNotification)
                friend.Message(_("FriendAddedNotification", friend.Id), player.Name);
            Interface.Oxide.NextTick(() => {
                Interface.Oxide.CallHook("FriendAdded", player, friend);
            });
            return true;
        }

        // Adds friend to player's friends list, by id, generic.
        bool AddFriend<T>(T playerId, T friendId) where T : struct => AddFriend(playerId.ToString(), friendId.ToString());

        // Removes friend from player's friends list, by id.
        bool RemoveFriend(string playerId, string friendId)
        {
            if (ReferenceEquals(playerId, null))
                throw new ArgumentNullException("playerId");
            if (ReferenceEquals(friendId, null))
                throw new ArgumentNullException("friendId");
            var player = covalence.Players.GetPlayer(playerId);
            if (player == null)
                return false;
            var friend = covalence.Players.GetPlayer(friendId);
            if (friend == null)
                return false;
            PlayerData data;
            if (playerData.TryGetValue(player.Id, out data) && data.Friends.Remove(friend.Id))
            {
                saveData();
                HashSet<string> reverseFriendData;
                if (reverseData.TryGetValue(friend.Id, out reverseFriendData))
                {
                    reverseFriendData.Remove(player.Id);
                    if (reverseFriendData.Count == 0)
                        reverseData.Remove(friend.Id);
                }
                if (configData.SendRemovedNotification)
                    friend.Message(_("FriendRemovedNotification", friend.Id), player.Name);
                Interface.Oxide.NextTick(() => {
                    Interface.Oxide.CallHook("FriendRemoved", player, friend);
                });
                return true;
            }
            return false;
        }

        // Removes friend from player's friends list, by id, generic.
        bool RemoveFriend<T>(T playerId, T friendId) where T : struct => RemoveFriend(playerId.ToString(), friendId.ToString());

        // Reusable empty collections meant to reduce allocations / garbage
        readonly string[] emptyStringArray = new string[0];
        
        // Gets an array of player's friends, by id.
        string[] GetFriends(string playerId)
        {
            if (ReferenceEquals(playerId, null))
                throw new ArgumentNullException("playerId");
            PlayerData data;
            if (playerData.TryGetValue(playerId, out data) && data.Friends.Count > 0)
                return data.Friends.ToArray();
            else
                return emptyStringArray;
        }

        // Gets an array of player's friends, by id, generic.
        T[] GetFriends<T>(T playerId) where T : struct => GetFriends(playerId.ToString()).Select(i => (T)Convert.ChangeType(i, typeof(T))).ToArray();

        // Gets an array of players who have added friend to their friends list, by id.
        string[] GetFriendsReverse(string friendId)
        {
            if (ReferenceEquals(friendId, null))
                throw new ArgumentNullException("friendId");
            HashSet<string> reverseFriendData;
            if (reverseData.TryGetValue(friendId, out reverseFriendData) && reverseFriendData.Count > 0)
                return reverseFriendData.ToArray();
            else
                return emptyStringArray;
        }

        // Gets an array of players who have added friend to their friends list, by id, generic.
        T[] GetFriendsReverse<T>(T friendId) where T : struct => GetFriendsReverse(friendId.ToString()).Select(i => (T)Convert.ChangeType(i, typeof(T))).ToArray();

        // Compatibility: ulong
        bool AddFriend(ulong playerId, ulong friendId) => AddFriend(playerId.ToString(), friendId.ToString());
        bool RemoveFriend(ulong playerId, ulong friendId) => HasFriend(playerId.ToString(), friendId.ToString());
        bool HasFriend(ulong playerId, ulong friendId) => HasFriend(playerId.ToString(), friendId.ToString());
        bool AreFriends(ulong playerId, ulong friendId) => AreFriends(playerId.ToString(), friendId.ToString());
        bool IsFriend(ulong playerId, ulong friendId) => HasFriend(friendId, playerId);
        public ulong[] GetFriendList(ulong playerId) => GetFriends(playerId);
        public ulong[] IsFriendOf(ulong friendId) => GetFriendsReverse(friendId);

        // Compatibility: string
        bool AddFriendS(string playerId, string friendId) => AddFriend(playerId, friendId);
        bool RemoveFriendS(string playerId, string friendId) => HasFriend(playerId, friendId);
        bool HasFriendS(string playerId, string friendId) => HasFriend(playerId, friendId);
        bool AreFriendsS(string playerId, string friendId) => AreFriends(playerId, friendId);
        bool IsFriendS(string playerId, string friendId) => HasFriend(friendId, playerId);
        public string[] GetFriendListS(string playerId) => GetFriends(playerId);
        public string[] IsFriendOfS(string friendId) => GetFriendsReverse(friendId);

        #endregion

        #region Game-specific: Rust

#if RUST
        void SendHelpText(BasePlayer player) => player.ChatMessage(_("HelpText", player.userID.ToString()));

        object OnTurretTarget(AutoTurret turret, BaseCombatEntity target)
        {
            BasePlayer player;
            return configData.Rust.ShareAutoTurrets && (player = (target as BasePlayer)) != null && HasFriend(turret.OwnerID, player.userID)
                ? @false // cancel targeting if ShareAutoTurrets is enabled and target is a friend of the turret's owner
                : null;  // otherwise use default behaviour
        }

        object OnPlayerAttack(BasePlayer attacker, HitInfo hit)
        {
            BasePlayer victim;
            return configData.DisableFriendlyFire && (victim = (hit.HitEntity as BasePlayer)) != null && attacker != victim && HasFriend(attacker.userID, victim.userID)
                ? @false // cancel attack if DisableFriendlyFire is enabled and victim is a friend of the attacker
                : null;  // otherwise use default behaviour
        }

        object CanUseDoor(BasePlayer player, CodeLock codeLock)
        {
            return configData.Rust.ShareCodeLocks && HasFriend(codeLock.GetParentEntity().OwnerID, player.userID)
                ? @true  // allow door usage if ShareCodeLocks is enabled and player is a friend of the door's owner
                : null;  // otherwise use default behaviour
        }
#endif

        #endregion

    }
}
