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

using Framework.ClientDB;
using Framework.Configuration;
using Framework.Constants;
using Framework.Constants.NetMessage;
using Framework.Database;
using Framework.Logging;
using Framework.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorldServer.Game.Packets.PacketHandler;
using WorldServer.Game.WorldEntities;
using WorldServer.Network;

namespace WorldServer.Game.PacketHandler
{
    public class CharacterHandler : Globals
    {
        [Opcode(ClientMessage.EnumCharacters, "16769")]
        public static void HandleEnumCharactersResult(ref PacketReader packet, ref WorldClass session)
        {
            // Set existing character from last world session to null
            session.Character = null;

            DB.Realms.Execute("UPDATE accounts SET online = 1 WHERE id = ?", session.Account.Id);

            SQLResult result = DB.Characters.Select("SELECT * FROM characters WHERE accountid = ?", session.Account.Id);

            PacketWriter enumCharacters = new PacketWriter(ServerMessage.EnumCharactersResult);
            BitPack BitPack = new BitPack(enumCharacters);

            BitPack.Write(0, 21);
            BitPack.Write(result.Count, 16);

            if (result.Count != 0)
            {
                for (int c = 0; c < result.Count; c++)
                {
                    string name         = result.Read<String>(c, "Name");
                    bool loginCinematic = result.Read<Boolean>(c, "LoginCinematic");

                    BitPack.Guid      = result.Read<UInt64>(c, "Guid");
                    BitPack.GuildGuid = result.Read<UInt64>(c, "GuildGuid");

                    BitPack.WriteGuildGuidMask(3, 2);
                    BitPack.WriteGuidMask(7, 5);
                    BitPack.Write((uint)UTF8Encoding.UTF8.GetBytes(name).Length, 6);
                    BitPack.WriteGuildGuidMask(0, 4);
                    BitPack.WriteGuidMask(3, 4, 6);
                    BitPack.WriteGuildGuidMask(1);
                    BitPack.WriteGuidMask(1, 2);
                    BitPack.WriteGuildGuidMask(5, 7);
                    BitPack.Write(loginCinematic);
                    BitPack.WriteGuildGuidMask(6);
                    BitPack.WriteGuidMask(0);
                }

                BitPack.Write(1);
                BitPack.Flush();

                for (int c = 0; c < result.Count; c++)
                {
                    string name       = result.Read<String>(c, "Name");
                    BitPack.Guid      = result.Read<UInt64>(c, "Guid");
                    BitPack.GuildGuid = result.Read<UInt64>(c, "GuildGuid");

                    enumCharacters.WriteUInt8(result.Read<Byte>(c, "HairStyle"));

                    BitPack.WriteGuildGuidBytes(0);
                    BitPack.WriteGuidBytes(1, 3);

                    enumCharacters.WriteFloat(result.Read<Single>(c, "Y"));

                    BitPack.WriteGuidBytes(4);

                    enumCharacters.WriteUInt8(result.Read<Byte>(c, "FacialHair"));
                    enumCharacters.WriteUInt32(result.Read<UInt32>(c, "CharacterFlags"));

                    // Not implanted
                    for (int j = 0; j < 23; j++)
                    {
                        enumCharacters.WriteUInt32(0);
                        enumCharacters.WriteUInt32(0);
                        enumCharacters.WriteUInt8(0);
                    }

                    BitPack.WriteGuildGuidBytes(1);

                    enumCharacters.WriteFloat(result.Read<Single>(c, "Z"));

                    BitPack.WriteGuildGuidBytes(6);

                    enumCharacters.WriteUInt32(result.Read<UInt32>(c, "PetDisplayId"));
                    enumCharacters.WriteUInt32(result.Read<UInt32>(c, "Zone"));
                    
                    BitPack.WriteGuildGuidBytes(5);

                    enumCharacters.WriteUInt8(result.Read<Byte>(c, "Face"));
                    enumCharacters.WriteUInt8(result.Read<Byte>(c, "Class"));

                    BitPack.WriteGuildGuidBytes(2);
                    BitPack.WriteGuidBytes(2, 5, 7, 0);

                    enumCharacters.WriteUInt8(result.Read<Byte>(c, "Level"));
                    enumCharacters.WriteUInt8(0);
                    enumCharacters.WriteUInt32(result.Read<UInt32>(c, "CustomizeFlags"));
                    enumCharacters.WriteUInt32(result.Read<UInt32>(c, "PetLevel"));
                    enumCharacters.WriteUInt32(result.Read<UInt32>(c, "Map"));
                    enumCharacters.WriteUInt32(result.Read<UInt32>(c, "PetFamily"));
                    enumCharacters.WriteFloat(result.Read<Single>(c, "X"));

                    BitPack.WriteGuildGuidBytes(4);

                    enumCharacters.WriteUInt8(result.Read<Byte>(c, "Skin"));

                    BitPack.WriteGuidBytes(6);

                    enumCharacters.WriteUInt8(result.Read<Byte>(c, "HairColor"));

                    BitPack.WriteGuildGuidBytes(7);

                    enumCharacters.WriteUInt8(result.Read<Byte>(c, "Gender"));

                    BitPack.WriteGuildGuidBytes(3);

                    enumCharacters.WriteString(name);
                    enumCharacters.WriteUInt8(result.Read<Byte>(c, "Race"));
                }
            }
            else
            {
                BitPack.Write(1);
                BitPack.Flush();
            };

            session.Send(ref enumCharacters);
        }

        [Opcode(ClientMessage.CreateCharacter, "16769")]
        public static void HandleCreateCharacter(ref PacketReader packet, ref WorldClass session)
        {
            BitUnpack BitUnpack = new BitUnpack(packet);

            packet.ReadByte();                      // Always 0
            byte facialHair = packet.ReadByte();
            byte skin = packet.ReadByte();
            byte hairStyle = packet.ReadByte();
            byte gender = packet.ReadByte();
            byte hairColor = packet.ReadByte();
            byte race = packet.ReadByte();
            byte pClass = packet.ReadByte();
            byte face = packet.ReadByte();

            uint nameLength = BitUnpack.GetBits<uint>(7);
            string name = Character.NormalizeName(packet.ReadString(nameLength));

            SQLResult result = DB.Characters.Select("SELECT * from characters WHERE name = ?", name);
            PacketWriter createChar = new PacketWriter(ServerMessage.CreateChar);

            if (result.Count != 0)
            {
                // Name already in use
                createChar.WriteUInt8(0x32);
                session.Send(ref createChar);
                return;
            }

            result = DB.Characters.Select("SELECT map, zone, posX, posY, posZ, posO FROM character_creation_data WHERE race = ? AND class = ?", race, pClass);
            if (result.Count == 0)
            {
                createChar.WriteUInt8(0x31);
                session.Send(ref createChar);
                return;
            }

            uint map = result.Read<uint>(0, "map");
            uint zone = result.Read<uint>(0, "zone");
            float posX = result.Read<float>(0, "posX");
            float posY = result.Read<float>(0, "posY");
            float posZ = result.Read<float>(0, "posZ");
            float posO = result.Read<float>(0, "posO");

            // Allow declined names for now
            var characterFlags = CharacterFlag.Decline;

            DB.Characters.Execute("INSERT INTO characters (name, accountid, realmId, race, class, gender, skin, zone, map, x, y, z, o, face, hairstyle, haircolor, facialhair, characterFlags) VALUES (" +
                                  "?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                                  name, session.Account.Id, WorldConfig.RealmId, race, pClass, gender, skin, zone, map, posX, posY, posZ, posO, face, hairStyle, hairColor, facialHair, characterFlags);

            // Success
            createChar.WriteUInt8(0x2F);
            session.Send(ref createChar);
        }

        [Opcode(ClientMessage.CharDelete, "16769")]
        public static void HandleCharDelete(ref PacketReader packet, ref WorldClass session)
        {
            bool[] guidMask = new bool[8];
            byte[] guidBytes = new byte[8];

            BitUnpack BitUnpack = new BitUnpack(packet);

            var unknown = BitUnpack.GetBit();

            guidMask[7] = BitUnpack.GetBit();
            guidMask[2] = BitUnpack.GetBit();
            guidMask[5] = BitUnpack.GetBit();
            guidMask[6] = BitUnpack.GetBit();
            guidMask[3] = BitUnpack.GetBit();
            guidMask[4] = BitUnpack.GetBit();
            guidMask[0] = BitUnpack.GetBit();
            guidMask[1] = BitUnpack.GetBit();

            if (guidMask[2]) guidBytes[2] = (byte)(packet.Read<byte>() ^ 1);
            if (guidMask[6]) guidBytes[6] = (byte)(packet.Read<byte>() ^ 1);
            if (guidMask[1]) guidBytes[1] = (byte)(packet.Read<byte>() ^ 1);
            if (guidMask[4]) guidBytes[4] = (byte)(packet.Read<byte>() ^ 1);
            if (guidMask[3]) guidBytes[3] = (byte)(packet.Read<byte>() ^ 1);
            if (guidMask[0]) guidBytes[0] = (byte)(packet.Read<byte>() ^ 1);
            if (guidMask[7]) guidBytes[7] = (byte)(packet.Read<byte>() ^ 1);
            if (guidMask[5]) guidBytes[5] = (byte)(packet.Read<byte>() ^ 1);

            var guid = BitConverter.ToUInt64(guidBytes, 0);

            PacketWriter deleteChar = new PacketWriter(ServerMessage.DeleteChar);
            deleteChar.WriteUInt8(0x47);
            session.Send(ref deleteChar);

            DB.Characters.Execute("DELETE FROM characters WHERE guid = ?", guid);
            DB.Characters.Execute("DELETE FROM character_spells WHERE guid = ?", guid);
            DB.Characters.Execute("DELETE FROM character_skills WHERE guid = ?", guid);
        }

        [Opcode(ClientMessage.GenerateRandomCharacterName, "16769")]
        public static void HandleGenerateRandomCharacterName(ref PacketReader packet, ref WorldClass session)
        {
            byte race = packet.ReadByte();
            byte gender = packet.ReadByte();

            List<string> names = CliDB.NameGen.Where(n => n.Race == race && n.Gender == gender).Select(n => n.Name).ToList();
            Random rand = new Random(Environment.TickCount);

            string NewName;
            SQLResult result;
            do
            {
                NewName = names[rand.Next(names.Count)];
                result = DB.Characters.Select("SELECT * FROM characters WHERE name = ?", NewName);
            }
            while (result.Count != 0);

            PacketWriter generateRandomCharacterNameResult = new PacketWriter(ServerMessage.GenerateRandomCharacterNameResult);
            BitPack BitPack = new BitPack(generateRandomCharacterNameResult);

            BitPack.Write<int>(NewName.Length, 14);
            BitPack.Write(true);
            BitPack.Flush();

            generateRandomCharacterNameResult.WriteString(NewName);
            session.Send(ref generateRandomCharacterNameResult);
        }

        [Opcode(ClientMessage.PlayerLogin, "16769")]
        public static void HandlePlayerLogin(ref PacketReader packet, ref WorldClass session)
        {
            byte[] guidMask = { 5, 0, 6, 2, 1, 3, 7, 4 };
            byte[] guidBytes = { 1, 5, 7, 0, 4, 6, 3, 2 };

            BitUnpack GuidUnpacker = new BitUnpack(packet);

            var unknown = packet.Read<float>();
            var guid = GuidUnpacker.GetPackedValue(guidMask, guidBytes);
            Log.Message(LogType.DEBUG, "Character with Guid: {0}, AccountId: {1} tried to enter the world.", guid, session.Account.Id);

            session.Character = new Character(guid);

            if (!WorldMgr.AddSession(guid, ref session))
            {
                Log.Message(LogType.ERROR, "A Character with Guid: {0} is already logged in", guid);
                return;
            }

            WorldMgr.WriteAccountDataTimes(AccountDataMasks.CharacterCacheMask, ref session);

            MiscHandler.HandleMessageOfTheDay(ref session);
            TimeHandler.HandleLoginSetTimeSpeed(ref session);
            SpecializationHandler.HandleUpdateTalentData(ref session);
            SpellHandler.HandleSendKnownSpells(ref session);
            MiscHandler.HandleUpdateActionButtons(ref session);

            if (session.Character.LoginCinematic)
                CinematicHandler.HandleStartCinematic(ref session);

            ObjectHandler.HandleUpdateObjectCreate(ref session);
        }
    }
}
