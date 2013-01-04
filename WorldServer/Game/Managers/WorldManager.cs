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
using Framework.Constants.ObjectSettings;
using Framework.Network.Packets;
using Framework.ObjectDefines;
using Framework.Singleton;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using WorldServer.Game.WorldEntities;
using WorldServer.Network;

namespace WorldServer.Game.Managers
{
    public sealed class WorldManager : SingletonBase<WorldManager>
    {
        public Dictionary<ulong, WorldClass> Sessions;
        public WorldClass Session { get; set; }

        WorldManager()
        {
            Sessions = new Dictionary<ulong, WorldClass>();

            StartRangeUpdateTimers();
        }

        public void AddSession(ulong guid, ref WorldClass session)
        {
            if (Sessions.ContainsKey(guid))
                Sessions.Remove(guid);

            Sessions.Add(guid, session);
        }

        public void DeleteSession(ulong guid)
        { 
            Sessions.Remove(guid);
        }

        public WorldClass GetSession(string name)
        {
            foreach (var s in Sessions)
                if (s.Value.Character.Name == name)
                    return s.Value;

            return null;
        }

        public WorldClass GetSession(ulong guid)
        {
            foreach (var s in Sessions)
                if (s.Value.Character.Guid == guid)
                    return s.Value;

            return null;
        }

        public void StartRangeUpdateTimers()
        {
            var updateTask = new Thread(UpdateTask);
            updateTask.IsBackground = true;
            updateTask.Start();
        }

        void UpdateTask()
        {
            while (true)
            {
                Thread.Sleep(50);

                foreach (var c in Sessions.ToList())
                {
                    var pChar = c.Value.Character;
                    var inRangeCSpawns = Globals.SpawnMgr.GetInRangeCreatures(pChar);
                    var count = inRangeCSpawns.Count();

                    if (count > 0)
                    {
                        UpdateFlag updateFlags = UpdateFlag.Alive | UpdateFlag.Rotation;

                        PacketWriter updateObject = new PacketWriter(LegacyMessage.UpdateObject);

                        updateObject.WriteUInt16((ushort)pChar.Map);
                        updateObject.WriteUInt32((uint)count);

                        foreach (var s in inRangeCSpawns)
                        {
                            WorldObject spawn = s.Key;

                            if (!pChar.InRangeObjects.ContainsKey(s.Key.Guid))
                            {
                                spawn.ToCreature().SetCreatureFields();

                                var data = s.Value;

                                updateObject.WriteUInt8(1);
                                updateObject.WriteGuid(spawn.Guid);
                                updateObject.WriteUInt8((byte)ObjectType.Unit);

                                Globals.WorldMgr.WriteUpdateObjectMovement(ref updateObject, ref spawn, updateFlags);

                                spawn.WriteUpdateFields(ref updateObject);
                                spawn.WriteDynamicUpdateFields(ref updateObject);

                                pChar.InRangeObjects.Add(spawn.Guid, spawn);
                            }
                        }

                        c.Value.Send(ref updateObject);
                    }

                    var inRangeGSpawns = Globals.SpawnMgr.GetInRangeGameObjects(pChar);
                    count = inRangeGSpawns.Count();

                    if (count > 0)
                    {
                        UpdateFlag updateFlags = UpdateFlag.Rotation | UpdateFlag.StationaryPosition;

                        PacketWriter updateObject = new PacketWriter(LegacyMessage.UpdateObject);

                        updateObject.WriteUInt16((ushort)pChar.Map);
                        updateObject.WriteUInt32((uint)count);

                        foreach (var s in inRangeGSpawns)
                        {
                            WorldObject spawn = s.Key;

                            if (!pChar.InRangeObjects.ContainsKey(spawn.Guid))
                            {
                                spawn.ToGameObject().SetGameObjectFields();

                                var data = s.Value;

                                updateObject.WriteUInt8(1);
                                updateObject.WriteGuid(spawn.Guid);
                                updateObject.WriteUInt8((byte)ObjectType.GameObject);

                                Globals.WorldMgr.WriteUpdateObjectMovement(ref updateObject, ref spawn, updateFlags);

                                spawn.WriteUpdateFields(ref updateObject);
                                spawn.WriteDynamicUpdateFields(ref updateObject);

                                pChar.InRangeObjects.Add(spawn.Guid, spawn);
                            }
                        }

                        c.Value.Send(ref updateObject);
                    }

                    var inRangeChars = GetInRangeCharacter(pChar);
                    count = inRangeChars.Count();

                    if (count > 0)
                    {
                        UpdateFlag updateFlags = UpdateFlag.Alive | UpdateFlag.Rotation;

                        PacketWriter updateObject = new PacketWriter(LegacyMessage.UpdateObject);

                        updateObject.WriteUInt16((ushort)pChar.Map);
                        updateObject.WriteUInt32((uint)count);

                        foreach (var ch in inRangeChars)
                        {
                            WorldObject iChar = ch.Value.Character;

                            if (!pChar.InRangeObjects.ContainsKey(iChar.Guid))
                            {
                                updateObject.WriteUInt8(1);
                                updateObject.WriteGuid(iChar.Guid);
                                updateObject.WriteUInt8(4);

                                updateFlags = UpdateFlag.Alive | UpdateFlag.Rotation;
                                WriteUpdateObjectMovement(ref updateObject, ref iChar, updateFlags);

                                iChar.WriteUpdateFields(ref updateObject);
                                iChar.WriteDynamicUpdateFields(ref updateObject);

                                ch.Value.Send(ref updateObject);

                                pChar.InRangeObjects.Add(iChar.Guid, iChar);
                            }
                        }

                        c.Value.Send(ref updateObject);
                    }

                    var outOfRangeCSpawns = Globals.SpawnMgr.GetOutOfRangeCreatures(pChar);
                    count = outOfRangeCSpawns.Count();

                    if (count > 0)
                    {
                        PacketWriter updateObject = new PacketWriter(LegacyMessage.UpdateObject);

                        updateObject.WriteUInt16((ushort)pChar.Map);
                        updateObject.WriteUInt32(1);
                        updateObject.WriteUInt8(3);
                        updateObject.WriteUInt32((uint)count);

                        foreach (var s in outOfRangeCSpawns)
                        {
                            WorldObject spawn = s.Key;

                            updateObject.WriteGuid(spawn.Guid);
                            pChar.InRangeObjects.Remove(spawn.Guid);
                        }

                        c.Value.Send(ref updateObject);
                    }

                    var outOfRangeGSpawns = Globals.SpawnMgr.GetOutOfRangeGameObjects(pChar);
                    count = outOfRangeGSpawns.Count();

                    if (count > 0)
                    {
                        PacketWriter updateObject = new PacketWriter(LegacyMessage.UpdateObject);

                        updateObject.WriteUInt16((ushort)pChar.Map);
                        updateObject.WriteUInt32(1);
                        updateObject.WriteUInt8(3);
                        updateObject.WriteUInt32((uint)count);

                        foreach (var s in outOfRangeGSpawns)
                        {
                            WorldObject spawn = s.Key;

                            updateObject.WriteGuid(spawn.Guid);
                            pChar.InRangeObjects.Remove(spawn.Guid);
                        }

                        c.Value.Send(ref updateObject);
                    }

                    var outOfRangeChars = GetOutOfRangeCharacter(pChar);
                    count = outOfRangeChars.Count();

                    if (count > 0)
                    {
                        PacketWriter updateObject = new PacketWriter(LegacyMessage.UpdateObject);

                        updateObject.WriteUInt16((ushort)pChar.Map);
                        updateObject.WriteUInt32(1);
                        updateObject.WriteUInt8(3);
                        updateObject.WriteUInt32((uint)count);

                        foreach (var s in outOfRangeChars)
                        {
                            WorldObject oChar = s.Value.Character;

                            updateObject.WriteGuid(oChar.Guid);
                            pChar.InRangeObjects.Remove(oChar.Guid);
                        }

                        c.Value.Send(ref updateObject);
                    }
                }
            }
        }

        public IEnumerable<KeyValuePair<ulong, WorldClass>> GetInRangeCharacter(WorldObject obj)
        {
            foreach (var c in Sessions.ToList())
                if (!obj.ToCharacter().InRangeObjects.ContainsKey(c.Key))
                    if (obj.CheckUpdateDistance(c.Value.Character))
                        yield return c;
        }

        public IEnumerable<KeyValuePair<ulong, WorldClass>> GetOutOfRangeCharacter(WorldObject obj)
        {
            foreach (var c in Sessions.ToList())
                if (obj.ToCharacter().InRangeObjects.ContainsKey(c.Key))
                    if (!obj.CheckUpdateDistance(c.Value.Character))
                        yield return c;
        }

        public void WriteAccountData(AccountDataMasks mask, ref WorldClass session)
        {
            PacketWriter accountInitialized = new PacketWriter(LegacyMessage.AccountDataInitialized);
            accountInitialized.WriteUnixTime();
            accountInitialized.WriteUInt8(0);
            accountInitialized.WriteUInt32((uint)mask);

            for (int i = 0; i <= 8; ++i)
                if (((int)mask & (1 << i)) != 0)
                    if (i == 1 && mask == AccountDataMasks.GlobalCacheMask)
                        accountInitialized.WriteUnixTime();
                    else
                        accountInitialized.WriteUInt32(0);

            session.Send(ref accountInitialized);
        }

        public void SendToAllInMap(ulong guid, PacketWriter packet)
        {
            var map = Sessions[guid].Character.Map;

            foreach (var s in Sessions)
            {
                if (s.Value.Character.Map != map)
                    continue;

                s.Value.Send(ref packet);
            }
        }

        public void SendToAllOtherInZone(ulong guid, PacketWriter packet)
        {
            var zone = Sessions[guid].Character.Zone;

            foreach (var s in Sessions)
            {
                if (s.Value.Character.Guid == guid || s.Value.Character.Zone != zone)
                    continue;

                s.Value.Send(ref packet);
            }
        }

        public void SendToAllOtherInMap(ulong guid, PacketWriter packet)
        {
            var map = Sessions[guid].Character.Map;

            foreach (var s in Sessions)
            {
                if (s.Value.Character.Guid == guid || s.Value.Character.Map != map)
                    continue;

                s.Value.Send(ref packet);
            }
        }

        public void WriteUpdateObjectMovement(ref PacketWriter packet, ref WorldObject wObject, UpdateFlag updateFlags)
        {
            ObjectMovementValues values = new ObjectMovementValues(updateFlags);
            BitPack BitPack = new BitPack(packet, wObject.Guid);

            BitPack.Write(0);                       // New in 5.1.0, 654, Unknown
            BitPack.Write(values.Bit0);
            BitPack.Write(values.HasRotation);
            BitPack.Write(values.HasTarget);
            BitPack.Write(values.Bit2);
            BitPack.Write(values.HasUnknown3);
            BitPack.Write(values.BitCounter, 24);
            BitPack.Write(values.HasUnknown);
            BitPack.Write(values.HasGoTransportPosition);
            BitPack.Write(values.HasUnknown2);
            BitPack.Write(0);                       // New in 5.1.0, 784, Unknown
            BitPack.Write(values.IsSelf);
            BitPack.Write(values.Bit1);
            BitPack.Write(values.IsAlive);
            BitPack.Write(values.Bit3);
            BitPack.Write(values.HasUnknown4);
            BitPack.Write(values.HasStationaryPosition);
            BitPack.Write(values.IsVehicle);
            BitPack.Write(values.BitCounter2, 21);
            BitPack.Write(values.HasAnimKits);

            if (values.IsAlive)
            {
                BitPack.WriteGuidMask(3);
                BitPack.Write(0);                   // IsInterpolated, not implanted
                BitPack.Write(1);                   // Unknown_Alive_2, Reversed
                BitPack.Write(0);                   // Unknown_Alive_4
                BitPack.WriteGuidMask(2);
                BitPack.Write(0);                   // Unknown_Alive_1
                BitPack.Write(1);                   // Pitch or splineElevation, not implanted
                BitPack.Write(true);                // MovementFlags2 are not implanted
                BitPack.WriteGuidMask(4, 5);
                BitPack.Write(0, 24);               // BitCounter_Alive_1
                BitPack.Write(1);                   // Pitch or splineElevation, not implanted
                BitPack.Write(!values.IsAlive);
                BitPack.Write(0);                   // Unknown_Alive_3
                BitPack.WriteGuidMask(0, 6, 7);
                BitPack.Write(values.IsTransport);
                BitPack.Write(!values.HasRotation);

                if (values.IsTransport)
                {
                    // Transports not implanted.
                }

                /* MovementFlags2 are not implanted
                 * if (movementFlag2 != 0)
                 *     BitPack.Write(0, 12);*/

                BitPack.Write(true);                // Movementflags are not implanted
                BitPack.WriteGuidMask(1);

                /* IsInterpolated, not implanted
                 * if (IsInterpolated)
                 * {
                 *     BitPack.Write(0);            // IsFalling
                 * }*/

                BitPack.Write(0);                   // HasSplineData, don't write simple basic splineData

                /* Movementflags are not implanted
                if (movementFlags != 0)
                    BitPack.Write((uint)movementFlags, 30);*/

                // Don't send basic spline data and disable advanced data
                // if (HasSplineData)
                //BitPack.Write(0);             // Disable advance splineData
            }

            BitPack.Flush();

            if (values.IsAlive)
            {
                packet.WriteFloat((float)MovementSpeed.FlyBackSpeed);

                // Don't send basic spline data
                /*if (HasSplineBasicData)
                {
                    // Advanced spline data not implanted
                    if (HasAdvancedSplineData)
                    {

                    }

                    packet.WriteFloat(character.X);
                    packet.WriteFloat(character.Y);
                    packet.WriteUInt32(0);
                    packet.WriteFloat(character.Z);
                }*/

                packet.WriteFloat((float)MovementSpeed.SwimSpeed);

                if (values.IsTransport)
                {
                    // Not implanted
                }

                BitPack.WriteGuidBytes(1);
                packet.WriteFloat((float)MovementSpeed.TurnSpeed);
                packet.WriteFloat(wObject.Position.Y);
                BitPack.WriteGuidBytes(3);
                packet.WriteFloat(wObject.Position.Z);
                packet.WriteFloat(wObject.Position.O);
                packet.WriteFloat((float)MovementSpeed.RunBackSpeed);
                BitPack.WriteGuidBytes(0, 6);
                packet.WriteFloat(wObject.Position.X);
                packet.WriteFloat((float)MovementSpeed.WalkSpeed);
                BitPack.WriteGuidBytes(5);
                packet.WriteUInt32(0);
                packet.WriteFloat((float)MovementSpeed.PitchSpeed);
                BitPack.WriteGuidBytes(2);
                packet.WriteFloat((float)MovementSpeed.RunSpeed);
                BitPack.WriteGuidBytes(7);
                packet.WriteFloat((float)MovementSpeed.SwimBackSpeed);
                BitPack.WriteGuidBytes(4);
                packet.WriteFloat((float)MovementSpeed.FlySpeed);
            }

            if (values.HasStationaryPosition)
            {
                packet.WriteFloat(wObject.Position.X);
                packet.WriteFloat(wObject.Position.O);
                packet.WriteFloat(wObject.Position.Y);
                packet.WriteFloat(wObject.Position.Z);
            }

            if (values.HasRotation)
                packet.WriteInt64(Quaternion.GetCompressed(wObject.Position.O));
        }
    }
}
