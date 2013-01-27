/*
 * Copyright (C) 2012-2013 Arctium <http://arctium.org>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Framework.Network.Packets;
using Framework.ObjectDefines;
using WorldServer.Game.WorldEntities;
using WorldServer.Network;

namespace WorldServer.Game.Packets.PacketHandler
{
    public class GossipHandler : Globals
    {
        [Opcode(ClientMessage.TalkToGossip, "16357")]
        public static void HandleTalkToGossip(ref PacketReader packet, ref WorldClass session)
        {
            var guid = packet.ReadUInt64();
            var gossipData = GossipMgr.GetGossip<Creature>(ObjectGuid.GetGuid(guid));

            if (gossipData != null)
            {
                PacketWriter gossipMessage = new PacketWriter(LegacyMessage.GossipMessage);

                gossipMessage.WriteUInt64(guid);
                gossipMessage.WriteInt32(gossipData.Id);
                gossipMessage.WriteInt32(gossipData.FriendshipFactionID);
                gossipMessage.WriteInt32(gossipData.TextID);
                gossipMessage.WriteInt32(gossipData.OptionsCount);
                gossipMessage.WriteInt32(gossipData.QuestsCount);

                session.Send(ref gossipMessage);
            }
        }

        [Opcode(ClientMessage.DBQueryBulk, "16357")]
        public static void HandleDBQueryBulk(ref PacketReader packet, ref WorldClass session)
        {
            var type = (DBTypes)packet.ReadUInt32();
            var unknown = packet.ReadInt32();
            var id = packet.ReadInt32();

            switch (type)
            {
                case DBTypes.BroadcastText:
                {
                    var broadCastText = GossipMgr.GetBroadCastText<Creature>(id);

                    PacketWriter dbReply = new PacketWriter(JAMCMessage.DBReply);
                    BitPack BitPack = new BitPack(dbReply);

                    var textLength = broadCastText.Text.Length;
                    var alternativeTextLength = broadCastText.AlternativeText.Length;
                    var size = 48;

                    if (textLength == 0 || alternativeTextLength == 0)
                        size += 1;
                    
                    size += textLength + alternativeTextLength;

                    dbReply.WriteUInt32((uint)size);
                    dbReply.WriteInt32(broadCastText.Id);
                    dbReply.WriteInt32(broadCastText.Language);

                    dbReply.WriteUInt16((ushort)broadCastText.Text.Length);
                    dbReply.WriteString(broadCastText.Text);

                    dbReply.WriteUInt16((ushort)broadCastText.AlternativeText.Length);
                    dbReply.WriteString(broadCastText.AlternativeText);

                    broadCastText.Emotes.ForEach(emote => dbReply.WriteInt32(emote));

                    dbReply.WriteUInt32(1);

                    dbReply.WriteUInt32(0);    // UnixTime, last change server side
                    dbReply.WriteUInt32((uint)DBTypes.BroadcastText);
                    dbReply.WriteInt32(broadCastText.Id);

                    session.Send(ref dbReply);
                    break;
                }
            }
        }
    }
}
