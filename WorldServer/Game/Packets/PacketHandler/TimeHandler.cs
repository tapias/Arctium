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
using WorldServer.Network;

namespace WorldServer.Game.Packets.PacketHandler
{
    public class TimeHandler : Globals
    {
        [Opcode(ClientMessage.ReadyForAccountDataTimes, "16733")]
        public static void HandleReadyForAccountDataTimes(ref PacketReader packet, ref WorldClass session)
        {
            WorldMgr.WriteAccountDataTimes(AccountDataMasks.GlobalCacheMask, ref session);
        }

        [Opcode(ClientMessage.UITimeRequest, "16733")]
        public static void HandleUITimeRequest(ref PacketReader packet, ref WorldClass session)
        {
            PacketWriter uiTime = new PacketWriter(ServerMessage.UITime);

            uiTime.WriteUnixTime();

            session.Send(ref uiTime);
        }

        [Opcode(ClientMessage.RealmSplit, "16733")]
        public static void HandleRealmSplit(ref PacketReader packet, ref WorldClass session)
        {
            uint realmSplitState = 0;
            var date = "01/01/01";

            PacketWriter realmSplit = new PacketWriter(ServerMessage.RealmSplit);
            BitPack BitPack = new BitPack(realmSplit);

            realmSplit.WriteUInt32(packet.ReadUInt32());
            realmSplit.WriteUInt32(realmSplitState);

            BitPack.Write(date.Length, 15);
            realmSplit.WriteString(date);

            session.Send(ref realmSplit);

            // Crash!!!
            // Wrong data sent...
            // AddonMgr.WriteAddonData(ref session);
        }

        public static void HandleLoginSetTimeSpeed(ref WorldClass session)
        {
            PacketWriter loginSetTimeSpeed = new PacketWriter(ServerMessage.LoginSetTimeSpeed);

            loginSetTimeSpeed.WriteInt32(0);
            loginSetTimeSpeed.WritePackedTime();
            loginSetTimeSpeed.WritePackedTime();
            loginSetTimeSpeed.WriteFloat(0.01666667f);
            loginSetTimeSpeed.WriteInt32(0);

            session.Send(ref loginSetTimeSpeed);
        }
    }
}
