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
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Oxide.Plugins
{
    [Info("Friends", "dcode", "1.0.0")]
    [Description("Universal friends plugin.")]
    public class Friends : CovalencePlugin
    {
        #region Config

        class ConfigData
        {
            // DO NOT EDIT! These are the defaults. Edit oxide/config/Friends.json instead!

            public int  MaxFriends = 30;
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

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new ConfigData(), true);
        }

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

        void loadData() => playerData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, PlayerData>>(Name);

        void loadConfig() => configData = Config.ReadObject<ConfigData>();

        void saveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, playerData);

        #endregion

        #region Hooks

        readonly object @true = true;
        readonly object @false = false;

        void Loaded()
        {
            loadConfig();
            loadData();
            registerMessages();
            if (playerData == null)
                playerData = new Dictionary<string, PlayerData>();
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

        readonly List<string> onlineListReuse = new List<string>(30);
        readonly List<string> offlineListReuse = new List<string>(30);

        [Command("friends")]
        void cmdFriends(IPlayer player, string command, string[] args)
        {
            PlayerData data;
            if (playerData.TryGetValue(player.Id, out data) && data.Friends.Count > 0)
            {
                player.Reply(_("List", player.Id));
                foreach (var friendId in data.Friends)
                {
                    // Sort friends by online status (must be mutual friends to show as online)
                    var friend = covalence.Players.GetPlayer(friendId);
                    if (friend != null)
                    {
                        if (friend.IsConnected && HasFriend(friend.Id, player.Id))
                            onlineListReuse.Add(friend.Name);
                        else
                            offlineListReuse.Add(friend.Name);
                    }
                    else
                    {
                        PlayerData friendData;
                        if (playerData.TryGetValue(friendId, out friendData))
                            offlineListReuse.Add(friendData.Name);
                        else
                            offlineListReuse.Add("#" + friendId);
                    }
                }
                foreach (var friendName in onlineListReuse)
                    player.Message(_("ListOnline", player.Id) + " " + friendName);
                onlineListReuse.Clear();
                foreach (var friendName in offlineListReuse)
                    player.Message(friendName);
                offlineListReuse.Clear();
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
            if (playerData.TryGetValue(player.Id, out data))
            {
                if (data.Friends.Count >= configData.MaxFriends)
                {
                    player.Reply(_("FriendlistFull", player.Id));
                    return;
                }
            }
            else if (configData.MaxFriends < 1)
            {
                player.Reply(_("FriendlistFull", player.Id));
                return;
            }
            else
                playerData[player.Id] = data = new PlayerData() { Name = player.Name, Friends = new HashSet<string>() };
            if (data.Friends.Add(friend.Id))
            {
                player.Reply(_("FriendAdded", player.Id), friend.Name);
                saveData();
            }
            else
            {
                player.Reply(_("AlreadyAFriend", player.Id), friend.Name);
            }
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
            {
                player.Reply(_("NoSuchPlayer", player.Id));
                return;
            }
            if (!RemoveFriend(player.Id, friend.Id))
                player.Reply(_("NoSuchFriend", player.Id));
            else
                player.Reply(_("FriendRemoved", player.Id), friend.Name);
        }

        #endregion

        #region API

        bool IsBattleLinkCompatible() => true;

        bool HasFriend(string playerId, string friendId)
        {
            PlayerData data;
            return playerData.TryGetValue(playerId, out data) && data.Friends.Contains(friendId);
        }

        bool HasFriendS(string playerId, string friendId) => HasFriend(playerId, friendId);

        bool HasFriendL(ulong playerId, ulong friendId) => HasFriend(playerId.ToString(), friendId.ToString());

        bool AreMutualFriends(string playerId, string friendId) => HasFriend(playerId, friendId) && HasFriend(friendId, playerId);

        bool AreMutualFriendsS(string playerId, string friendId) => AreMutualFriends(playerId, friendId);

        bool AreMutualFriendsL(ulong playerId, ulong friendId) => AreMutualFriends(playerId.ToString(), friendId.ToString());

        bool AddFriend(string playerId, string friendId)
        {
            var player = covalence.Players.GetPlayer(playerId);
            if (player == null)
                return false;
            var friend = covalence.Players.GetPlayer(friendId);
            if (friend == null)
                return false;
            PlayerData data;
            if (playerData.TryGetValue(player.Id, out data))
                data.Friends.Add(friend.Id);
            else
                data = playerData[player.Id] = new PlayerData() { Name = player.Name, Friends = new HashSet<string>() { friend.Id } };
            if (!playerData.TryGetValue(friend.Id, out data)) // also add a blank reverse entry, remembering the friend's name
                playerData[friend.Id] = new PlayerData() { Name = friend.Name, Friends = new HashSet<string>() };
            saveData();
            if (configData.SendAddedNotification)
                friend.Message(_("FriendAddedNotification", friend.Id), player.Name);
            Interface.Oxide.NextTick(() => {
                Interface.Oxide.CallHook("FriendAdded", player, friend);
            });
            return true;
        }

        bool AddFriendS(string playerId, string friendId) => AddFriend(playerId, friendId);

        bool AddFriendL(ulong playerId, ulong friendId) => AddFriend(playerId.ToString(), friendId.ToString());

        bool RemoveFriend(string playerId, string friendId)
        {
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
                if (configData.SendRemovedNotification)
                    friend.Message(_("FriendRemovedNotification", friend.Id), player.Name);
                Interface.Oxide.NextTick(() => {
                    Interface.Oxide.CallHook("FriendRemoved", player, friend);
                });
                return true;
            }
            return false;
        }

        bool RemoveFriendS(string playerId, string friendId) => RemoveFriend(playerId, friendId);

        bool RemoveFriendL(ulong playerId, ulong friendId) => RemoveFriend(playerId.ToString(), friendId.ToString());

        readonly IPlayer[] EmptyFriendsList = new IPlayer[0];

        IPlayer[] GetFriends(string playerId)
        {
            PlayerData data;
            if (playerData.TryGetValue(playerId, out data) && data.Friends.Count > 0)
            {
                var friends = new List<IPlayer>();
                foreach (var friendId in data.Friends)
                {
                    var friend = covalence.Players.GetPlayer(friendId);
                    if (friend != null)
                        friends.Add(friend);
                }
                return friends.ToArray();
            }
            return EmptyFriendsList;
        }

        IPlayer[] GetFriends(ulong playerId) => GetFriends(playerId.ToString());

        public string GetPlayerName(string playerId)
        {
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

        #endregion

        #region Game-specific: Rust

#if RUST
        void SendHelpText(BasePlayer player) => player.ChatMessage(_("HelpText", player.userID.ToString()));

        object OnTurretTarget(AutoTurret turret, BaseCombatEntity target)
        {
            BasePlayer player;
            return configData.Rust.ShareAutoTurrets && (player = (target as BasePlayer)) != null && HasFriendL(turret.OwnerID, player.userID)
                ? @false // cancel targeting if ShareAutoTurrets is enabled and target is a friend of the turret's owner
                : null;  // otherwise use default behaviour
        }

        object OnPlayerAttack(BasePlayer attacker, HitInfo hit)
        {
            BasePlayer victim;
            return configData.DisableFriendlyFire && (victim = (hit.HitEntity as BasePlayer)) != null && attacker != victim && HasFriendL(attacker.userID, victim.userID)
                ? @false // cancel attack if DisableFriendlyFire is enabled and victim is a friend of the attacker
                : null;  // otherwise use default behaviour
        }

        object CanUseDoor(BasePlayer player, CodeLock codeLock)
        {
            return configData.Rust.ShareCodeLocks && HasFriendL(codeLock.GetParentEntity().OwnerID, player.userID)
                ? @true  // allow door usage if ShareCodeLocks is enabled and player is a friend of the door's owner
                : null;  // otherwise use default behaviour
        }
#endif

        #endregion

    }
}
