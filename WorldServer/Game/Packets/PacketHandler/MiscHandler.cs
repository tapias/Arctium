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

using System;
using System.Linq;
using Framework.Constants;
using Framework.Logging;
using Framework.Network.Packets;
using WorldServer.Game.ObjectDefines;
using WorldServer.Network;
using Framework.Database;
using Framework.ObjectDefines;

namespace WorldServer.Game.Packets.PacketHandler
{
    public class MiscHandler : Globals
    {
        public static void HandleMessageOfTheDay(ref WorldClass session)
        {
            PacketWriter motd = new PacketWriter(LegacyMessage.MessageOfTheDay);
            motd.WriteUInt32(3);

            motd.WriteCString("Arctium MoP test");
            motd.WriteCString("Welcome to our MoP server test.");
            motd.WriteCString("Your development team =)");
            session.Send(ref motd);
        }

        [Opcode(ClientMessage.Ping, "16357")]
        public static void HandlePong(ref PacketReader packet, ref WorldClass session)
        {
            uint sequence = packet.ReadUInt32();
            uint latency = packet.ReadUInt32();

            PacketWriter pong = new PacketWriter(JAMCCMessage.Pong);
            pong.WriteUInt32(sequence);

            session.Send(ref pong);
        }

        [Opcode(ClientMessage.LogDisconnect, "16357")]
        public static void HandleDisconnectReason(ref PacketReader packet, ref WorldClass session)
        {
            var pChar = session.Character;
            uint disconnectReason = packet.ReadUInt32();

            if (pChar != null)
                WorldMgr.DeleteSession(pChar.Guid);

            DB.Realms.Execute("UPDATE accounts SET online = 0 WHERE id = ?", session.Account.Id);

            Log.Message(LogType.DEBUG, "Account with Id {0} disconnected. Reason: {1}", session.Account.Id, disconnectReason);
        }

        public static void HandleUpdateClientCacheVersion(ref WorldClass session)
        {
            PacketWriter cacheVersion = new PacketWriter(LegacyMessage.UpdateClientCacheVersion);

            cacheVersion.WriteUInt32(0);

            session.Send(ref cacheVersion);
        }

        [Opcode(ClientMessage.LoadingScreenNotify, "16357")]
        public static void HandleLoadingScreenNotify(ref PacketReader packet, ref WorldClass session)
        {
            BitUnpack BitUnpack = new BitUnpack(packet);

            uint mapId = packet.ReadUInt32();
            bool loadingScreenState = BitUnpack.GetBit();

            Log.Message(LogType.DEBUG, "Loading screen for map '{0}' is {1}.", mapId, loadingScreenState ? "enabled" : "disabled");
        }

        [Opcode(ClientMessage.ViolenceLevel, "16357")]
        public static void HandleViolenceLevel(ref PacketReader packet, ref WorldClass session)
        {
            byte violenceLevel = packet.ReadUInt8();

            Log.Message(LogType.DEBUG, "Violence level from account '{0} (Id: {1})' is {2}.", session.Account.Name, session.Account.Id, (ViolenceLevel)violenceLevel);
        }

        [Opcode(ClientMessage.ActivePlayer, "16357")]
        public static void HandleActivePlayer(ref PacketReader packet, ref WorldClass session)
        {
            byte active = packet.ReadUInt8();    // Always 0

            Log.Message(LogType.DEBUG, "Player {0} (Guid: {1}) is active.", session.Character.Name, session.Character.Guid);
        }

        [Opcode(ClientMessage.ZoneUpdate, "16357")]
        public static void HandleZoneUpdate(ref PacketReader packet, ref WorldClass session)
        {
            var pChar = session.Character;

            uint zone = packet.ReadUInt32();

            ObjectMgr.SetZone(ref pChar, zone);
        }

        [Opcode(ClientMessage.SetSelection, "16357")]
        public static void HandleSetSelection(ref PacketReader packet, ref WorldClass session)
        {
            byte[] guidMask = { 3, 1, 7, 2, 6, 4, 0, 5 };
            byte[] guidBytes = { 4, 1, 5, 2, 6, 7, 0, 3 };

            BitUnpack GuidUnpacker = new BitUnpack(packet);

            ulong fullGuid = GuidUnpacker.GetGuid(guidMask, guidBytes);
            ulong guid = ObjectGuid.GetGuid(fullGuid);

            if (session.Character != null)
            {
                var sess = WorldMgr.GetSession(session.Character.Guid);

                if (sess != null)
                    sess.Character.TargetGuid = fullGuid;

                if (guid == 0)
                    Log.Message(LogType.DEBUG, "Character (Guid: {0}) removed current selection.", session.Character.Guid);
                else
                    Log.Message(LogType.DEBUG, "Character (Guid: {0}) selected a {1} (Guid: {2}, Id: {3}).", session.Character.Guid, ObjectGuid.GetGuidType(fullGuid), guid, ObjectGuid.GetId(fullGuid));
            }
        }

        [Opcode(ClientMessage.SetActionButton, "16357")]
        public static void HandleSetActionButton(ref PacketReader packet, ref WorldClass session)
        {
            var pChar = session.Character;

            byte[] actionMask = { 4, 0, 3, 7, 1, 6, 2, 5 };
            byte[] actionBytes = { 3, 0, 1, 4, 7, 2, 6, 5 };

            var slotId = packet.ReadByte();
            
            BitUnpack actionUnpacker = new BitUnpack(packet);
            
            var actionId = actionUnpacker.GetValue(actionMask, actionBytes);
            
            if (actionId == 0)
            {
                var action = pChar.ActionButtons.Where(button => button.SlotId == slotId && button.SpecGroup == pChar.ActiveSpecGroup).Select(button => button).First();
                ActionMgr.RemoveActionButton(pChar, action, true);
                Log.Message(LogType.DEBUG, "Character (Guid: {0}) removed action button {1} from slot {2}.", pChar.Guid, actionId, slotId);
                return;
            }

            var newAction = new ActionButton
            {
                Action = actionId,
                SlotId = slotId,
                SpecGroup = pChar.ActiveSpecGroup
            };

            ActionMgr.AddActionButton(pChar, newAction, true);
            Log.Message(LogType.DEBUG, "Character (Guid: {0}) added action button {1} to slot {2}.", pChar.Guid, actionId, slotId);
        }

        public static void HandleUpdateActionButtons(ref WorldClass session)
        {
            var pChar = session.Character;

            PacketWriter updateActionButtons = new PacketWriter(JAMCMessage.UpdateActionButtons);
            BitPack BitPack = new BitPack(updateActionButtons);

            const int buttonCount = 132;
            var buttons = new byte[buttonCount][];

            byte[] buttonMask = { 4, 0, 7, 2, 6, 3, 1, 5 };
            byte[] buttonBytes = { 0, 3, 5, 7, 6, 1, 4, 2 };

            var actions = ActionMgr.GetActionButtons(pChar, pChar.ActiveSpecGroup);
            
            for (int i = 0; i < buttonCount; i++)
                if (actions.Any(action => action.SlotId == i))
                    buttons[i] = BitConverter.GetBytes((ulong)actions.Where(action => action.SlotId == i).Select(action => action.Action).First());
                else
                    buttons[i] = new byte[8];

            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < buttonCount; j++)
                {
                    if (i < 8)
                        BitPack.Write(buttons[j][buttonMask[i]]);
                    else if (i < 16)
                    {
                        if (buttons[j][buttonBytes[i - 8]] != 0)
                            updateActionButtons.WriteUInt8((byte)(buttons[j][buttonBytes[i - 8]] ^ 1));
                    }
                }
            }

            // Packet Type (NYI)
            // 0 - Initial packet on Login (no verification) / 1 - Verify spells on switch (Spec change) / 2 - Clear Action Buttons (Spec change)
            updateActionButtons.WriteInt8(0);

            session.Send(ref updateActionButtons);
        }
    }
}
