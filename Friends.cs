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

        private ConfigData configData;

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(new ConfigData(), true);
        }

        #endregion

        #region Language

        private void registerMessages() => lang.RegisterMessages(new Dictionary<string, string>
        {
			// Command replies
			{ "PlayerNotFound", "There is no player matching that name." },
            { "NotOnFriendlist", "You don't have a friend matching that name." },
            { "FriendAdded", "{0} is now one of your friends." },
            { "FriendRemoved", "{0} is no longer one of your friends." },
            { "AlreadyAFriend", "{0} is already one of your friends." },
            { "CantAddSelf", "You cannot add yourself to your friends." },
            { "NoFriends", "You haven't added any friends, yet." },
            { "List", "You have {0} of a maximum of {1} friends:" },
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

        private string _(string key, string playerId) => lang.GetMessage(key, this, playerId);

        #endregion

        #region Persistence

        class PlayerData { public string Name; public HashSet<string> Friends; }

        private Dictionary<string, PlayerData> Data { get; private set; }

        void loadData() => Data = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<string, PlayerData>>("Friends");

        void loadConfig() => configData = Config.ReadObject<ConfigData>();

        void saveData() => Interface.Oxide.DataFileSystem.WriteObject("Friends", Data);

        #endregion

        #region Hooks

        void Loaded()
        {
            registerMessages();
            loadConfig();
            loadData();
            if (Data == null)
                Data = new Dictionary<string, PlayerData>();
        }

        void OnUserConnected(IPlayer player)
        {
            if (!configData.SendOnlineNotification)
                return;
            PlayerData data;
            if (!Data.TryGetValue(player.Id, out data) || data.Friends.Count == 0)
                return;
            foreach (var friendId in data.Friends)
            {
                var friend = covalence.Players.GetPlayer(friendId);
                if (friend != null && friend.IsConnected)
                    friend.Message(_("FriendOnlineNotification", friend.Id), player.Name);
            }
        }

        void OnUserDisconnected(IPlayer player)
        {
            if (!configData.SendOnlineNotification)
                return;
            PlayerData data;
            if (!Data.TryGetValue(player.Id, out data) || data.Friends.Count == 0)
                return;
            foreach (var friendId in data.Friends)
            {
                var friend = covalence.Players.GetPlayer(friendId);
                if (friend != null && friend.IsConnected)
                    friend.Message(_("FriendOfflineNotification", friend.Id), player.Name);
            }
        }

        [Command("/friends")]
        void cmdFriends(IPlayer player, string command, string[] args)
        {
            PlayerData data;
            int count;
            if (!Data.TryGetValue(player.Id, out data) || (count = data.Friends.Count) == 0)
            {
                player.Reply(_("NoFriends", player.Id));
                return;
            }
            var sb = new StringBuilder();
            sb.Append(string.Format(_("List", player.Id), count))
              .AppendLine();
            foreach (var friendId in data.Friends)
            {
                string friendName;
                var friend = covalence.Players.GetPlayer(friendId);
                if (friend == null)
                {
                    PlayerData friendData;
                    if (Data.TryGetValue(friendId, out friendData))
                        friendName = friendData.Name;
                    else
                        friendName = "#" + friendId;
                }
                else
                    friendName = friend.Name;
                sb.Append("  ")
                  .Append(friendName)
                  .AppendLine();
            }
            sb.Append(_("UsageAdd", player.Id))
              .AppendLine()
              .Append(_("UsageRemove", player.Id));
            player.Reply(sb.ToString());
        }

        [Command("/addfriend")]
        void cmdAddFriend(IPlayer player, string command, string[] args)
        {
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
            if (!Data.TryGetValue(player.Id, out data))
                Data[player.Id] = data = new PlayerData() { Name = player.Name, Friends = new HashSet<string>() };
            if (data.Friends.Count >= configData.MaxFriends)
            {
                player.Reply(_("FriendlistFull", player.Id));
                return;
            }
            if (!data.Friends.Add(friend.Id))
            {
                player.Reply(_("AlreadyAFriend", player.Id), friend.Name);
                return;
            }
            player.Reply(_("FriendAdded", player.Id), friend.Name);
            saveData();
            CallHook("FriendAdded", player, friend);
        }

        [Command("/removefriend", "/deletefriend")]
        void cmdRemoveFriend(IPlayer player, string command, string[] args)
        {
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
            return Data.TryGetValue(playerId, out data) && data.Friends.Contains(friendId);
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
            if (!Data.TryGetValue(player.Id, out data))
                data = Data[player.Id] = new PlayerData() { Name = player.Name, Friends = new HashSet<string>() { friend.Id } };
            else
                data.Friends.Add(friend.Id);
            if (!Data.TryGetValue(friend.Id, out data)) // also add a blank reverse entry, remembering the friend's name
                Data[friend.Id] = new PlayerData() { Name = friend.Name, Friends = new HashSet<string>() };
            saveData();
            Interface.Oxide.CallHook("FriendAdded", player, friend);
            if (configData.SendAddedNotification)
                friend.Message(_("FriendAddedNotification", friend.Id), player.Name);
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
            if (!Data.TryGetValue(player.Id, out data) || !data.Friends.Contains(friend.Id))
                return false;
            data.Friends.Remove(friend.Id);
            saveData();
            Interface.Oxide.CallHook("FriendRemoved", player, friend);
            if (configData.SendRemovedNotification)
                friend.Message(_("FriendRemovedNotification", friend.Id), player.Name);
            return true;
        }

        bool RemoveFriendS(string playerId, string friendId) => RemoveFriend(playerId, friendId);

        bool RemoveFriendL(ulong playerId, ulong friendId) => RemoveFriend(playerId.ToString(), friendId.ToString());

        private readonly IPlayer[] EmptyFriendsList = new IPlayer[0];

        IPlayer[] GetFriends(string playerId)
        {
            PlayerData data;
            if (!Data.TryGetValue(playerId, out data))
                return EmptyFriendsList;
            var friends = new List<IPlayer>();
            foreach (var friendId in data.Friends)
            {
                var friend = covalence.Players.GetPlayer(friendId);
                if (friend != null)
                    friends.Add(friend);
            }
            return friends.ToArray();
        }

        IPlayer[] GetFriends(ulong playerId) => GetFriends(playerId.ToString());

        #endregion

        #region Game-specific: Rust

#if RUST
        private readonly FieldInfo CodeLock_whitelistPlayers = typeof(CodeLock).GetField("whitelistPlayers", BindingFlags.Instance | BindingFlags.NonPublic);

        void SendHelpText(BasePlayer player) => player.ChatMessage(_("HelpText", player.userID.ToString()));

        object OnTurretSetTarget(AutoTurret turret, BaseCombatEntity target)
        {
            if (!configData.Rust.ShareAutoTurrets || !(target is BasePlayer))
                return null;
            var player = (BasePlayer)target;
            if (turret.IsAuthed(player) || HasFriendL(turret.OwnerID, player.userID))
                return false;
            return null;
        }

        object CanUseDoor(BasePlayer player, BaseLock codelock)
        {
            if (!configData.Rust.ShareCodeLocks || !(codelock is CodeLock))
                return null;
            if (((IList<ulong>)CodeLock_whitelistPlayers.GetValue(codelock)).Contains(player.userID) || HasFriendL(codelock.GetParentEntity().OwnerID, player.userID))
                return true;
            return true;
        }

        void onAttackShared(BasePlayer attacker, BasePlayer victim, HitInfo hit)
        {
            if (attacker == victim)
                return;
            if (!HasFriendL(attacker.userID, victim.userID))
                return;
            hit.damageTypes = new Rust.DamageTypeList();
            hit.DidHit = false;
            hit.HitEntity = null;
            hit.Initiator = null;
            hit.DoHitEffects = false;
        }

        void OnPlayerAttack(BasePlayer attacker, HitInfo hit)
        {
            if (!configData.DisableFriendlyFire)
                return;
            if (hit.HitEntity is BasePlayer)
                onAttackShared(attacker, hit.HitEntity as BasePlayer, hit);
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hit)
        {
            if (!configData.DisableFriendlyFire)
                return;
            if (entity is BasePlayer && hit.Initiator is BasePlayer)
                onAttackShared(hit.Initiator as BasePlayer, entity as BasePlayer, hit);
        }
#endif

        #endregion

    }
}
