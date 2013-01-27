using Framework.Database;
using Framework.Logging;
using Framework.Singleton;
using System;
using System.Collections.Concurrent;
using WorldServer.Game.WorldEntities;

namespace WorldServer.Game.Managers
{
    public class GossipManager : SingletonBase<GossipManager>
    {
        ConcurrentDictionary<UInt64, Gossip> CreatureGossips;
        ConcurrentDictionary<UInt64, Gossip> GameObjectGossips;

        GossipManager()
        {
            CreatureGossips = new ConcurrentDictionary<UInt64, Gossip>();
            GameObjectGossips = new ConcurrentDictionary<UInt64, Gossip>();

            LoadGossips();
        }

        public Gossip GetGossip<T>(ulong guid)
        {
            Gossip gossip = null;

            if (typeof(T) == typeof(Creature))
                CreatureGossips.TryGetValue(guid, out gossip);
            else
                GameObjectGossips.TryGetValue(guid, out gossip);

            return gossip;
        }

        public BroadcastText GetBroadCastText<T>(int id)
        {
            if (typeof(T) == typeof(Creature))
            {
                foreach (var g in CreatureGossips)
                    if (g.Value.BroadCastText.Id == id)
                        return g.Value.BroadCastText;
            }
            else
            {
                foreach (var g in GameObjectGossips)
                    if (g.Value.BroadCastText.Id == id)
                        return g.Value.BroadCastText;
            }

            return null;
        }

        public void LoadGossips()
        {
            SQLResult result = DB.World.Select("SELECT * FROM creature_gossips cg RIGHT JOIN gossip_data gd ON cg.GossipDataId = gd.Id RIGHT JOIN broadcast_texts bt " +
                                               "ON cg.BroadcastTextId = bt.Id WHERE cg.GossipDataId IS NOT NULL AND cg.BroadcastTextId IS NOT NULL");

            for (int i = 0; i < result.Count; i++)
            {
                var guid       = result.Read<UInt64>(i, "Guid");

                var gossipData = new Gossip
                {
                    Id                  = result.Read<Int32>(i, "GossipDataId"),
                    FriendshipFactionID = result.Read<Int32>(i, "FriendshipFactionID"),
                    TextID              = result.Read<Int32>(i, "TextID"),
                    OptionsCount        = result.Read<Int32>(i, "OptionsCount"),
                    QuestsCount         = result.Read<Int32>(i, "QuestsCount")
                };

                gossipData.BroadCastText = new BroadcastText
                {
                    Id              = result.Read<Int32>(i, "BroadCastTextID"),
                    Language        = result.Read<Int32>(i, "Language"),
                    Text            = result.Read<string>(i, "Text"),
                    AlternativeText = result.Read<string>(i, "AlternativeText")
                };

                for (int j = 0; j < gossipData.BroadCastText.Emotes.Capacity; j++)
                    gossipData.BroadCastText.Emotes.Add(result.Read<Int32>(0, "Emote" + j));

                CreatureGossips.TryAdd(guid, gossipData);
            }

            Log.Message(LogType.DB, "Loaded {0} creature gossips.", CreatureGossips.Count);

            result = DB.World.Select("SELECT * FROM gameobject_gossips gg RIGHT JOIN gossip_data gd ON gg.GossipDataId = gd.Id RIGHT JOIN broadcast_texts bt " +
                                     "ON gg.BroadcastTextId = bt.Id WHERE gg.GossipDataId IS NOT NULL AND gg.BroadcastTextId IS NOT NULL");

            for (int i = 0; i < result.Count; i++)
            {
                var guid       = result.Read<UInt64>(i, "Guid");
                
                 var gossipData = new Gossip
                {
                    Id                  = result.Read<Int32>(i, "GossipDataId"),
                    FriendshipFactionID = result.Read<Int32>(i, "FriendshipFactionID"),
                    TextID              = result.Read<Int32>(i, "TextID"),
                    OptionsCount        = result.Read<Int32>(i, "OptionsCount"),
                    QuestsCount         = result.Read<Int32>(i, "QuestsCount")
                };

                gossipData.BroadCastText = new BroadcastText
                {
                    Id              = result.Read<Int32>(i, "BroadCastTextID"),
                    Language        = result.Read<Int32>(i, "Language"),
                    Text            = result.Read<string>(i, "Text"),
                    AlternativeText = result.Read<string>(i, "AlternativeText")
                };

                for (int j = 0; j < gossipData.BroadCastText.Emotes.Capacity; j++)
                    gossipData.BroadCastText.Emotes.Add(result.Read<Int32>(0, "Emote" + j));

                GameObjectGossips.TryAdd(guid, gossipData);
            }

            Log.Message(LogType.DB, "Loaded {0} gameobject gossips.", GameObjectGossips.Count);
        }
    }
}
