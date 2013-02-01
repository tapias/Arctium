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
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.DBC;
using Framework.Logging;
using Framework.Network.Packets;
using WorldServer.Network;

namespace WorldServer.Game.Packets.PacketHandler
{
    public class SpecializationHandler : Globals
    {
        [Opcode(ClientMessage.SetSpecialization, "16357")]
        public static void HandleSetSpecialization(ref PacketReader packet, ref WorldClass session)
        {
            var pChar = session.Character;

            uint specGroupId = packet.ReadUInt32();

            uint specId = SpecializationMgr.GetSpecIdByGroup(pChar, (byte)specGroupId);

            // check if new spec is primary or secondary
            if (pChar.SpecGroupCount == 1 && pChar.PrimarySpec == 0)
            {
                pChar.ActiveSpecGroup = 0;
                pChar.PrimarySpec = (ushort)specId;
            }
            else
            {
                pChar.ActiveSpecGroup = 1;
                pChar.SecondarySpec = (ushort)specId;
                pChar.SpecGroupCount = (byte)(pChar.SpecGroupCount + 1);
            }

            SpecializationMgr.SaveSpecInfo(pChar);

            SendSpecializationSpells(ref session);

            HandleTalentUpdate(ref session);

            pChar.SetUpdateField<Int32>((int)PlayerFields.CurrentSpecID, (int)pChar.GetActiveSpecId());
            ObjectHandler.HandleUpdateObjectValues(ref session);

            Log.Message(LogType.DEBUG, "Character (Guid: {0}) choosed spectialization {1} for spec group {2}.", pChar.Guid, pChar.GetActiveSpecId(), pChar.ActiveSpecGroup);
        }

        [Opcode(ClientMessage.LearnTalents, "16357")]
        public static void HandleLearnTalents(ref PacketReader packet, ref WorldClass session)
        {
            var pChar = session.Character;
            var talentSpells = new List<uint>();

            BitUnpack BitUnpack = new BitUnpack(packet);

            uint talentCount = BitUnpack.GetBits<uint>(25);

            for (int i = 0; i < talentCount; i++)
            {
                var talentId = packet.ReadUInt16();
                SpecializationMgr.AddTalent(pChar, pChar.ActiveSpecGroup, talentId, true);

                talentSpells.Add(DBCStorage.TalentStorage.LookupByKey(talentId).SpellId);
            }

            HandleTalentUpdate(ref session);

            pChar.SetUpdateField<Int32>((int)PlayerFields.SpellCritPercentage + 0, SpecializationMgr.GetUnspentTalentRowCount(pChar), 0);
            ObjectHandler.HandleUpdateObjectValues(ref session);

            // we need to send a single packet for every talent spell - stupid blizz
            foreach (var talentSpell in talentSpells)
                SpellHandler.HandleLearnedSpells(ref session, new List<uint>(1) { talentSpell });

            Log.Message(LogType.DEBUG, "Character (Guid: {0}) learned {1} talents.", pChar.Guid, talentCount);
        }

        public static void HandleTalentUpdate(ref WorldClass session)
        {
            var pChar = session.Character;

            const byte glyphCount = 6;

            PacketWriter writer = new PacketWriter(LegacyMessage.TalentUpdate);

            writer.WriteUInt8((byte)pChar.SpecGroupCount);      // Spec Count (Default 1)
            writer.WriteUInt8((byte)pChar.ActiveSpecGroup);     // Active Spec (0 or 1)

            for (int i = 0; i < pChar.SpecGroupCount; i++)
            {
                var specId = (i == 0) ? pChar.PrimarySpec : pChar.SecondarySpec;
                writer.WriteUInt32(specId);                     // Spec Id

                var talents = SpecializationMgr.GetTalentsBySpecGroup(pChar, (byte)i);

                writer.WriteUInt8((byte)talents.Count);         // Spent Talent Row Count
                for (int j = 0; j < talents.Count; j++)
                    writer.WriteUInt16(talents[j].Id);          // Talent Id

                writer.WriteUInt8(glyphCount);                  // Glyph Count - NYI
                for (int j = 0; j < glyphCount; j++)
                    writer.WriteInt16(0);                       // Glyph Id - NYI
            }

            session.Send(ref writer);
        }

        public static void SendSpecializationSpells(ref WorldClass session)
        {
            var specSpells = SpecializationMgr.GetSpecializationSpells(session.Character);
            var newSpells = specSpells.Select(specializationSpell => specializationSpell.SpellId).ToList();

            if (newSpells.Count > 0)
                SpellHandler.HandleLearnedSpells(ref session, newSpells);
        }
    }
}
