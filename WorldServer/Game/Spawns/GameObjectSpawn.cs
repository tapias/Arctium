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
using Framework.Database;
using Framework.Logging;
using Framework.Network.Packets;
using Framework.ObjectDefines;
using System;
using WorldServer.Game.WorldEntities;

namespace WorldServer.Game.Spawns
{
    public class GameObjectSpawn : WorldObject
    {
        public Int32 Id;
        public UInt32 FactionTemplate;
        public Byte AnimProgress;
        public Boolean Activated;
        public GameObject GameObject;

        public GameObjectSpawn(int updateLength = (int)GameObjectFields.End) : base(updateLength) { }

        public static ulong GetLastGuid()
        {
            SQLResult result = DB.World.Select("SELECT * FROM `gameobject_spawns` ORDER BY `guid` DESC LIMIT 1");
            if (result.Count != 0)
                return result.Read<ulong>(0, "guid");

            return 0;
        }

        public void CreateFullGuid()
        {
            Guid = new ObjectGuid(Guid, Id, HighGuidType.GameObject).Guid;
        }

        public void CreateData(GameObject gameobject)
        {
            GameObject = gameobject;
        }

        public bool AddToDB()
        {
            if (DB.World.Execute("INSERT INTO gameobject_spawns (Id, Map, X, Y, Z, O) VALUES (?, ?, ?, ?, ?, ?)", Id, Map, Position.X, Position.Y, Position.Z, Position.O))
            {
                Log.Message(LogType.DB, "Gameobject (Id: {0}) successfully spawned (Guid: {1})", Id, Guid);
                return true;
            }

            return false;
        }

        public void AddToWorld()
        {
            CreateFullGuid();
            CreateData(GameObject);

            Globals.SpawnMgr.AddSpawn(this, ref GameObject);

            WorldObject obj = this;
            UpdateFlag updateFlags = UpdateFlag.Rotation | UpdateFlag.StationaryPosition;

            foreach (var v in Globals.WorldMgr.Sessions)
            {
                Character pChar = v.Value.Character;

                if (pChar.CheckUpdateDistance(this))
                {
                    PacketWriter updateObject = new PacketWriter(LegacyMessage.UpdateObject);

                    updateObject.WriteUInt16((ushort)Map);
                    updateObject.WriteUInt32(1);

                    WorldMgr.WriteCreateObject(ref updateObject, obj, updateFlags, ObjectType.GameObject);

                    v.Value.Send(ref updateObject);
                }
            }
        }

        public override void SetUpdateFields()
        {
            // ObjectFields
            SetUpdateField<UInt64>((int)ObjectFields.Guid, Guid);
            SetUpdateField<UInt64>((int)ObjectFields.Data, 0);
            SetUpdateField<Int32>((int)ObjectFields.Type, 0x21);
            SetUpdateField<Int32>((int)ObjectFields.Entry, Id);
            SetUpdateField<Single>((int)ObjectFields.Scale, GameObject.Stats.Size);

            // GameObjectFields
            SetUpdateField<UInt64>((int)GameObjectFields.CreatedBy, 0);
            SetUpdateField<Int32>((int)GameObjectFields.DisplayID, GameObject.Stats.DisplayInfoId);
            SetUpdateField<Int32>((int)GameObjectFields.Flags, GameObject.Stats.Flags);
            SetUpdateField<Single>((int)GameObjectFields.ParentRotation, 0);
            SetUpdateField<Single>((int)GameObjectFields.ParentRotation + 1, 0);
            SetUpdateField<Single>((int)GameObjectFields.ParentRotation + 2, 0);
            SetUpdateField<Single>((int)GameObjectFields.ParentRotation + 3, 1);
            SetUpdateField<Byte>((int)GameObjectFields.AnimProgress, AnimProgress);
            SetUpdateField<Byte>((int)GameObjectFields.AnimProgress, 0, 1);
            SetUpdateField<Byte>((int)GameObjectFields.AnimProgress, 255, 2);
            SetUpdateField<Byte>((int)GameObjectFields.AnimProgress, 255, 3);
            SetUpdateField<UInt32>((int)GameObjectFields.FactionTemplate, FactionTemplate);
            SetUpdateField<Int32>((int)GameObjectFields.Level, 0);
            SetUpdateField<Byte>((int)GameObjectFields.PercentHealth, Convert.ToByte(Activated));
            SetUpdateField<Byte>((int)GameObjectFields.PercentHealth, (byte)GameObject.Stats.Type, 1);
            SetUpdateField<Byte>((int)GameObjectFields.PercentHealth, 0, 2);
            SetUpdateField<Byte>((int)GameObjectFields.PercentHealth, 255, 3);
        }
    }
}
