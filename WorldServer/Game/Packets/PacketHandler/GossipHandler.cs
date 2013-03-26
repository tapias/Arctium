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
using Framework.Constants.NetMessage;
using Framework.Network.Packets;
using Framework.ObjectDefines;
using WorldServer.Game.WorldEntities;
using WorldServer.Network;

namespace WorldServer.Game.Packets.PacketHandler
{
    public class GossipHandler : Globals
    {
        [Opcode(ClientMessage.CliTalkToGossip, "16769")]
        public static void HandleTalkToGossip(ref PacketReader packet, ref WorldClass session)
        {
            BitUnpack BitUnpack = new BitUnpack(packet);

            byte[] guidMask = { 2, 1, 7, 3, 6, 0, 4, 5 };
            byte[] guidBytes = { 5, 3, 6, 2, 7, 0, 4, 1 };

            var guid = BitUnpack.GetPackedValue(guidMask, guidBytes);
            var gossipData = GossipMgr.GetGossip<Creature>(ObjectGuid.GetGuid(guid));

            if (gossipData != null)
            {
                PacketWriter gossipMessage = new PacketWriter(ServerMessage.GossipMessage);
                BitPack BitPack = new BitPack(gossipMessage, guid);

                BitPack.WriteGuidMask(0, 5);
                BitPack.Write(gossipData.OptionsCount, 20);
                BitPack.WriteGuidMask(6, 1);

                for (int i = 0; i < gossipData.OptionsCount; i++)
                {
                    // OptionsCount not supported.
                }

                BitPack.WriteGuidMask(2);
                BitPack.Write(gossipData.QuestsCount, 19);
                BitPack.WriteGuidMask(4);

                for (int i = 0; i < gossipData.QuestsCount; i++)
                {
                    // QuestsCount not supported.
                }

                BitPack.WriteGuidMask(3, 7);
                BitPack.Flush();

                for (int i = 0; i < gossipData.OptionsCount; i++)
                {
                    // OptionsCount not supported.
                }

                for (int i = 0; i < gossipData.QuestsCount; i++)
                {
                    // QuestsCount not supported.
                }

                BitPack.WriteGuidBytes(5, 2, 6, 0);

                gossipMessage.WriteInt32(gossipData.Id);

                BitPack.WriteGuidBytes(4, 7);

                gossipMessage.WriteInt32(gossipData.TextID);

                BitPack.WriteGuidBytes(3, 1);

                gossipMessage.WriteInt32(gossipData.FriendshipFactionID);

                session.Send(ref gossipMessage);
            }
        }
    }
}
