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

using Framework.Constants.Movement;
using Framework.Constants.NetMessage;
using Framework.Network.Packets;
using Framework.ObjectDefines;
using System;
using WorldServer.Game.WorldEntities;
using WorldServer.Network;

namespace WorldServer.Game.Packets.PacketHandler
{
    public class MoveHandler : Globals
    {
        [Opcode(ClientMessage.MoveStartForward, "16769")]
        [Opcode(ClientMessage.MoveStartBackward, "16769")]
        [Opcode(ClientMessage.MoveStop, "16769")]
        [Opcode(ClientMessage.MoveJump, "16769")]
        [Opcode(ClientMessage.MoveStartTurnLeft, "16769")]
        [Opcode(ClientMessage.MoveStartTurnRight, "16769")]
        [Opcode(ClientMessage.MoveStopTurn, "16769")]
        [Opcode(ClientMessage.MoveFallLand, "16769")]
        [Opcode(ClientMessage.MoveHeartbeat, "16769")]
        public static void HandleMove(ref PacketReader packet, ref WorldClass session)
        {
            ObjectMovementValues movementValues = new ObjectMovementValues();
            BitUnpack BitUnpack = new BitUnpack(packet);

            bool[] guidMask = new bool[8];
            byte[] guidBytes = new byte[8];

            Vector4 vector = new Vector4()
            {
                X = packet.ReadFloat(),
                Y = packet.ReadFloat(),
                Z = packet.ReadFloat()
            };

            guidMask[0] = BitUnpack.GetBit();
            guidMask[3] = BitUnpack.GetBit();

            bool HasPitch = !BitUnpack.GetBit();

            guidMask[2] = BitUnpack.GetBit();

            bool HasSplineElevation = !BitUnpack.GetBit();

            bool Unknown = BitUnpack.GetBit();
            bool Unknown2 = BitUnpack.GetBit();

            guidMask[6] = BitUnpack.GetBit();
            guidMask[7] = BitUnpack.GetBit();
            guidMask[5] = BitUnpack.GetBit();

            movementValues.HasMovementFlags = !BitUnpack.GetBit();
            movementValues.HasRotation = !BitUnpack.GetBit();

            bool Unknown3 = BitUnpack.GetBit();

            movementValues.HasMovementFlags2 = !BitUnpack.GetBit();
            movementValues.IsAlive = !BitUnpack.GetBit();

            guidMask[1] = BitUnpack.GetBit();

            movementValues.IsTransport = BitUnpack.GetBit();

            guidMask[4] = BitUnpack.GetBit();

            movementValues.IsInterpolated = BitUnpack.GetBit();
            bool HasTime = !BitUnpack.GetBit();

            uint counter = BitUnpack.GetBits<uint>(22);

            if (movementValues.IsInterpolated)
                movementValues.IsInterpolated2 = BitUnpack.GetBit();

            if (movementValues.HasMovementFlags)
                movementValues.MovementFlags = (MovementFlag)BitUnpack.GetBits<uint>(30);

            if (movementValues.HasMovementFlags2)
                movementValues.MovementFlags2 = (MovementFlag2)BitUnpack.GetBits<uint>(13);

            if (guidMask[0]) guidBytes[0] = (byte)(packet.ReadUInt8() ^ 1);
            if (guidMask[2]) guidBytes[2] = (byte)(packet.ReadUInt8() ^ 1);

            for (int i = 0; i < counter; i++)
                packet.ReadUInt32();

            if (guidMask[7]) guidBytes[7] = (byte)(packet.ReadUInt8() ^ 1);
            if (guidMask[6]) guidBytes[6] = (byte)(packet.ReadUInt8() ^ 1);
            if (guidMask[1]) guidBytes[1] = (byte)(packet.ReadUInt8() ^ 1);
            if (guidMask[4]) guidBytes[4] = (byte)(packet.ReadUInt8() ^ 1);
            if (guidMask[3]) guidBytes[3] = (byte)(packet.ReadUInt8() ^ 1);
            if (guidMask[5]) guidBytes[5] = (byte)(packet.ReadUInt8() ^ 1);

            if (HasSplineElevation)
                packet.ReadFloat();

            /*if (movementValues.IsTransport)
            {

            }*/

            if (movementValues.IsInterpolated)
            {
                if (movementValues.IsInterpolated2)
                {
                    packet.ReadFloat();
                    packet.ReadFloat();
                    packet.ReadFloat();
                }

                packet.ReadUInt32();
                packet.ReadFloat();
            }

            if (HasPitch)
                packet.ReadFloat();

            if (movementValues.IsAlive)
                movementValues.Time = packet.ReadUInt32();

            if (movementValues.HasRotation)
                vector.O = packet.ReadFloat();

            if (HasTime)
                movementValues.Time = packet.ReadUInt32();

            var guid = BitConverter.ToUInt64(guidBytes, 0);
            HandleMoveUpdate(guid, movementValues, vector);
        }

        public static void HandleMoveUpdate(ulong guid, ObjectMovementValues movementValues, Vector4 vector)
        {
            PacketWriter moveUpdate = new PacketWriter(ServerMessage.MoveUpdate);
            BitPack BitPack = new BitPack(moveUpdate, guid);

            BitPack.WriteGuidMask(2);
            BitPack.Write(0);
            BitPack.WriteGuidMask(1);
            BitPack.Write(!movementValues.HasMovementFlags2);
            BitPack.Write(true);
            BitPack.Write(true);
            BitPack.Write<uint>(0, 22);
            BitPack.WriteGuidMask(3);
            BitPack.Write(!movementValues.IsAlive);
            BitPack.Write(!movementValues.HasMovementFlags);
            BitPack.WriteGuidMask(6);

            if (movementValues.HasMovementFlags2)
                BitPack.Write((uint)movementValues.MovementFlags2, 13);

            BitPack.WriteGuidMask(4, 7);
            BitPack.Write(movementValues.IsInterpolated);
            BitPack.Write(0);

            if (movementValues.HasMovementFlags)
                BitPack.Write((uint)movementValues.MovementFlags, 30);

            BitPack.Write(!movementValues.HasRotation);
            BitPack.Write(movementValues.IsTransport);
            BitPack.WriteGuidMask(5);

            if (movementValues.IsInterpolated)
                BitPack.Write(movementValues.IsInterpolated2);

            BitPack.Write(true);
            BitPack.Write(0);
            BitPack.WriteGuidMask(0);

            BitPack.Flush();

            moveUpdate.WriteFloat(vector.Z);

            BitPack.WriteGuidBytes(3);

            moveUpdate.WriteFloat(vector.X);

            BitPack.WriteGuidBytes(0, 7);

            moveUpdate.WriteFloat(vector.Y);

            BitPack.WriteGuidBytes(5);

            if (movementValues.IsInterpolated)
            {
                moveUpdate.WriteFloat(0);

                if (movementValues.IsInterpolated2)
                {
                    moveUpdate.WriteFloat(0);
                    moveUpdate.WriteFloat(0);
                    moveUpdate.WriteFloat(0);
                }

                moveUpdate.WriteUInt32(0);
            }

            BitPack.WriteGuidBytes(6, 2);

            if (movementValues.IsAlive)
                moveUpdate.WriteUInt32(movementValues.Time);

            BitPack.WriteGuidBytes(1, 4);

            if (movementValues.HasRotation)
                moveUpdate.WriteFloat(vector.O);

            var session = WorldMgr.GetSession(guid);
            if (session != null)
            {
                Character pChar = session.Character;

                ObjectMgr.SetPosition(ref pChar, vector, false);
                WorldMgr.SendToInRangeCharacter(pChar, moveUpdate);
            }
        }

        public static void HandleMoveSetWalkSpeed(ref WorldClass session, float speed = 2.5f)
        {
            PacketWriter setWalkSpeed = new PacketWriter(ServerMessage.MoveSetWalkSpeed);
            BitPack BitPack = new BitPack(setWalkSpeed, session.Character.Guid);

            BitPack.WriteGuidMask(5, 1, 4, 2, 7, 6, 0, 3);
            BitPack.Flush();

            BitPack.WriteGuidBytes(6, 7);

            setWalkSpeed.WriteFloat(speed);

            BitPack.WriteGuidBytes(2, 3, 0);

            setWalkSpeed.WriteUInt32(0);

            BitPack.WriteGuidBytes(1, 5, 4);

            session.Send(ref setWalkSpeed);
        }

        public static void HandleMoveSetRunSpeed(ref WorldClass session, float speed = 7f)
        {
            PacketWriter setRunSpeed = new PacketWriter(ServerMessage.MoveSetRunSpeed);
            BitPack BitPack = new BitPack(setRunSpeed, session.Character.Guid);

            setRunSpeed.WriteFloat(speed);
            setRunSpeed.WriteUInt32(0);

            BitPack.WriteGuidMask(7, 0, 6, 1, 3, 2, 4, 5);
            BitPack.Flush();

            BitPack.WriteGuidBytes(2, 5, 0, 1, 4, 7, 3, 6);

            session.Send(ref setRunSpeed);
        }

        public static void HandleMoveSetSwimSpeed(ref WorldClass session, float speed = 4.72222f)
        {
            PacketWriter setSwimSpeed = new PacketWriter(ServerMessage.MoveSetSwimSpeed);
            BitPack BitPack = new BitPack(setSwimSpeed, session.Character.Guid);

            BitPack.WriteGuidMask(3, 7, 2, 0, 1, 4, 5, 6);
            BitPack.Flush();


            BitPack.WriteGuidBytes(5, 3, 4, 7, 6);

            setSwimSpeed.WriteFloat(speed);
            setSwimSpeed.WriteUInt32(0);

            BitPack.WriteGuidBytes(1, 0, 2);

            session.Send(ref setSwimSpeed);
        }

        public static void HandleMoveSetFlightSpeed(ref WorldClass session, float speed = 7f)
        {
            PacketWriter setFlightSpeed = new PacketWriter(ServerMessage.MoveSetFlightSpeed);
            BitPack BitPack = new BitPack(setFlightSpeed, session.Character.Guid);

            BitPack.WriteGuidMask(6, 1, 0, 2, 5, 4, 7, 3);
            BitPack.Flush();

            BitPack.WriteGuidBytes(7, 2, 4, 6);

            setFlightSpeed.WriteUInt32(0);
            setFlightSpeed.WriteFloat(speed);

            BitPack.WriteGuidBytes(1, 0, 5, 3);

            session.Send(ref setFlightSpeed);
        }

        public static void HandleMoveSetCanFly(ref WorldClass session)
        {
            PacketWriter moveSetCanFly = new PacketWriter(ServerMessage.MoveSetCanFly);
            BitPack BitPack = new BitPack(moveSetCanFly, session.Character.Guid);


            BitPack.WriteGuidMask(5, 4, 6, 2, 3, 7, 1, 0);
            BitPack.Flush();

            BitPack.WriteGuidBytes(7, 6, 5, 2, 4, 3, 1, 0);
            
            moveSetCanFly.WriteUInt32(0);

            session.Send(ref moveSetCanFly);
        }

        public static void HandleMoveUnsetCanFly(ref WorldClass session)
        {
            PacketWriter unsetCanFly = new PacketWriter(ServerMessage.MoveUnsetCanFly);
            BitPack BitPack = new BitPack(unsetCanFly, session.Character.Guid);

            unsetCanFly.WriteUInt32(0);

            BitPack.WriteGuidMask(1, 5, 7, 0, 6, 2, 4, 3);
            BitPack.Flush();

            BitPack.WriteGuidBytes(4, 0, 2, 7, 6, 3, 1, 5);

            session.Send(ref unsetCanFly);
        }

        public static void HandleMoveTeleport(ref WorldClass session, Vector4 vector)
        {
            bool IsTransport = false;
            bool Unknown = false;

            PacketWriter moveTeleport = new PacketWriter(ServerMessage.MoveTeleport);
            BitPack BitPack = new BitPack(moveTeleport, session.Character.Guid);

            moveTeleport.WriteUInt32(0);
            moveTeleport.WriteFloat(vector.O);
            moveTeleport.WriteFloat(vector.X);
            moveTeleport.WriteFloat(vector.Z);
            moveTeleport.WriteFloat(vector.Y);

            BitPack.WriteGuidMask(2, 3);
            BitPack.Write(IsTransport);

            if (IsTransport)
                BitPack.WriteTransportGuidMask(2, 4, 3, 1, 7, 6, 5, 0);

            BitPack.WriteGuidMask(1, 7, 4, 0);
            BitPack.Write(Unknown);
            BitPack.WriteGuidMask(5);

            // Unknown bits
            if (Unknown)
            {
                BitPack.Write(0);
                BitPack.Write(0);
            }

            BitPack.WriteGuidMask(6);

            BitPack.Flush();

            BitPack.WriteGuidBytes(3, 2);

            if (IsTransport)
                BitPack.WriteTransportGuidBytes(2, 7, 1, 4, 5, 0, 6, 3);

            if (Unknown)
                moveTeleport.WriteUInt8(0);

            BitPack.WriteGuidBytes(7, 4, 6, 5, 0, 1);

            session.Send(ref moveTeleport);
        }

        public static void HandleTransferPending(ref WorldClass session, uint mapId)
        {
            bool Unknown = false;
            bool IsTransport = false;

            PacketWriter transferPending = new PacketWriter(ServerMessage.TransferPending);
            BitPack BitPack = new BitPack(transferPending);

            transferPending.WriteUInt32(mapId);

            BitPack.Write(Unknown);
            BitPack.Write(IsTransport);

            if (Unknown)
                transferPending.WriteUInt32(0);

            if (IsTransport)
            {
                transferPending.WriteUInt32(0);
                transferPending.WriteUInt32(0);
            }
            
            session.Send(ref transferPending);
        }

        public static void HandleNewWorld(ref WorldClass session, Vector4 vector, uint mapId)
        {
            PacketWriter newWorld = new PacketWriter(ServerMessage.NewWorld);

            newWorld.WriteUInt32(mapId);
            newWorld.WriteFloat(vector.Y);
            newWorld.WriteFloat(vector.X);
            newWorld.WriteFloat(vector.O);
            newWorld.WriteFloat(vector.Z);

            session.Send(ref newWorld);
        }
    }
}
