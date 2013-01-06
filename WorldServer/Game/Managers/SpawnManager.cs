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

using Framework.Database;
using Framework.Logging;
using Framework.ObjectDefines;
using Framework.Singleton;
using System;
using System.Collections.Generic;
using WorldServer.Game.Spawns;
using WorldServer.Game.WorldEntities;

namespace WorldServer.Game.Managers
{
    public sealed class SpawnManager : SingletonBase<SpawnManager>
    {
        public Dictionary<CreatureSpawn, Creature> CreatureSpawns;
        public Dictionary<GameObjectSpawn, GameObject> GameObjectSpawns;

        SpawnManager()
        {
            CreatureSpawns = new Dictionary<CreatureSpawn, Creature>();
            GameObjectSpawns = new Dictionary<GameObjectSpawn, GameObject>();

            Initialize();
        }

        public void Initialize()
        {
            LoadCreatureSpawns();
            LoadGameObjectSpawns();
        }

        public void AddSpawn(CreatureSpawn spawn, ref Creature data)
        {
            CreatureSpawns.Add(spawn, data);
        }

        public void RemoveSpawn(CreatureSpawn spawn)
        {
            CreatureSpawns.Remove(spawn);
            DB.World.Execute("DELETE FROM creature_spawns WHERE Guid = ?", ObjectGuid.GetGuid(spawn.Guid));
        }

        public CreatureSpawn FindSpawn(ulong guid)
        {
            foreach (var c in CreatureSpawns)
                if (c.Key.Guid == guid)
                    return c.Key;

            return null;
        }

        public IEnumerable<KeyValuePair<CreatureSpawn, Creature>> GetInRangeCreatures(WorldObject obj)
        {
            foreach (var c in CreatureSpawns)
                if (!obj.ToCharacter().InRangeObjects.ContainsKey(c.Key.Guid))
                    if (obj.CheckUpdateDistance(c.Key))
                        yield return c;
        }

        public IEnumerable<KeyValuePair<GameObjectSpawn, GameObject>> GetInRangeGameObjects(WorldObject obj)
        {
            foreach (var g in GameObjectSpawns)
                if (!obj.ToCharacter().InRangeObjects.ContainsKey(g.Key.Guid))
                    if (obj.CheckUpdateDistance(g.Key))
                        yield return g;
        }

        public IEnumerable<KeyValuePair<CreatureSpawn, Creature>> GetOutOfRangeCreatures(WorldObject obj)
        {
            foreach (var c in CreatureSpawns)
                if (obj.ToCharacter().InRangeObjects.ContainsKey(c.Key.Guid))
                    if (!obj.CheckUpdateDistance(c.Key))
                        yield return c;
        }

        public IEnumerable<KeyValuePair<GameObjectSpawn, GameObject>> GetOutOfRangeGameObjects(WorldObject obj)
        {
            foreach (var g in GameObjectSpawns)
                if (obj.ToCharacter().InRangeObjects.ContainsKey(g.Key.Guid))
                    if (!obj.CheckUpdateDistance(g.Key))
                        yield return g;
        }

        public void LoadCreatureSpawns()
        {
            SQLResult result = DB.World.Select("SELECT * FROM creature_spawns");

            for (int i = 0; i < result.Count; i++)
            {
                var guid = result.Read<UInt64>(i, "Guid");
                var id = result.Read<Int32>(i, "Id");

                Creature data = Globals.DataMgr.FindCreature(id);
                if (data == null)
                {
                    Log.Message(LogType.ERROR, "Loading a creature spawn (Guid: {0}) with non-existing stats (Id: {1}) skipped.", guid, id);
                    continue;
                }

                CreatureSpawn spawn = new CreatureSpawn()
                {
                    Guid = guid,
                    Id   = id,
                    
                    Map = result.Read<UInt32>(i, "Map"),

                    Position = new Vector4()
                    {
                        X = result.Read<Single>(i, "X"),
                        Y = result.Read<Single>(i, "Y"),
                        Z = result.Read<Single>(i, "Z"),
                        O = result.Read<Single>(i, "O")
                    },
                };

                spawn.CreateFullGuid();
                spawn.CreateData(data);

                AddSpawn(spawn, ref data);
            }

            Log.Message(LogType.DB, "Loaded {0} creature spawns.", CreatureSpawns.Count);
        }

        public void AddSpawn(GameObjectSpawn spawn, ref GameObject data)
        {
            GameObjectSpawns.Add(spawn, data);
        }

        public void RemoveSpawn(GameObjectSpawn spawn)
        {
            GameObjectSpawns.Remove(spawn);
            DB.World.Execute("DELETE FROM creature_spawns WHERE Guid = ?", ObjectGuid.GetGuid(spawn.Guid));
        }

        public GameObjectSpawn FindSpawn(GameObjectSpawn spawn)
        {
            foreach (var c in GameObjectSpawns)
                if (c.Key.Guid == spawn.Guid)
                    return c.Key;

            return null;
        }

        public void LoadGameObjectSpawns()
        {
            SQLResult result = DB.World.Select("SELECT * FROM gameobject_spawns");

            for (int i = 0; i < result.Count; i++)
            {
                var guid = result.Read<UInt64>(i, "Guid");
                var id = result.Read<Int32>(i, "Id");

                GameObject data = Globals.DataMgr.FindGameObject(id);
                if (data == null)
                {
                    Log.Message(LogType.ERROR, "Loading a gameobject spawn (Guid: {0}) with non-existing stats (Id: {1}) skipped.", guid, id);
                    continue;
                }

                GameObjectSpawn spawn = new GameObjectSpawn()
                {
                    Guid = guid,
                    Id   = id,

                    Map = result.Read<UInt32>(i, "Map"),

                    Position = new Vector4()
                    {
                        X = result.Read<Single>(i, "X"),
                        Y = result.Read<Single>(i, "Y"),
                        Z = result.Read<Single>(i, "Z"),
                        O = result.Read<Single>(i, "O")
                    },

                    FactionTemplate = result.Read<UInt32>(i, "FactionTemplate"),
                    AnimProgress    = result.Read<Byte>(i, "AnimProgress"),
                    Activated       = result.Read<bool>(i, "Activated"),
                };

                spawn.CreateFullGuid();
                spawn.CreateData(data);

                AddSpawn(spawn, ref data);
            }

            Log.Message(LogType.DB, "Loaded {0} gameobject spawns.", GameObjectSpawns.Count);
        }
    }
}
