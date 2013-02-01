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

using System.Linq;
using System.Collections.Generic;
using Framework.Constants;
using Framework.DBC;
using Framework.Database;
using Framework.Singleton;
using WorldServer.Game.WorldEntities;

namespace WorldServer.Game.Managers
{
    public class SpecializationManager : SingletonBase<SpecializationManager>
    {
        SpecializationManager() { }

        public void LoadTalents(Character pChar)
        {
            SQLResult result = DB.Characters.Select("SELECT * FROM character_talents WHERE guid = ? ORDER BY specGroup ASC", pChar.Guid);

            if (result.Count != 0)
            {
                for (int i = 0; i < result.Count; i++)
                    AddTalent(pChar, result.Read<byte>(i, "specGroup"), result.Read<ushort>(i, "talentId"));
            }
        }

        public List<SpecializationSpell> GetSpecializationSpells(Character pChar)
        {
            if (pChar.GetActiveSpecId() == 0)
                return null;

            var knownSpecSpells = new List<SpecializationSpell>();
            var specSpells = DBCStorage.SpecializationSpellStorage.Where(specSpell => specSpell.Value.SpecId == pChar.GetActiveSpecId()).ToList();

            foreach (var specSpell in specSpells)
            {
                var spellLevelId = DBCStorage.SpellStorage.LookupByKey(specSpell.Value.SpellId).SpellLevelsId;
                var spellBaseLevel = DBCStorage.SpellLevelStorage.LookupByKey(spellLevelId).BaseLevel;
                if (pChar.Level >= spellBaseLevel)
                    knownSpecSpells.Add(specSpell.Value);
            }

            return knownSpecSpells;
        }

        public List<uint> GetTalentSpells(Character pChar, byte specGroup)
        {
            if (GetSpentTalentRowCount(pChar, specGroup) == 0)
                return null;

            var talents = GetTalentsBySpecGroup(pChar, specGroup);

            return talents.Select(t => DBCStorage.TalentStorage.LookupByKey(t.Id).SpellId).ToList();
        }

        public void AddTalent(Character pChar, byte specGroup, ushort talentId, bool addToDb = false)
        {
            var newTalent = new ObjectDefines.Talent
            {
                Id = talentId,
                SpecGroup = specGroup
            };

            pChar.TalentList.Add(newTalent);

            if (addToDb)
                DB.Characters.Execute("INSERT INTO character_talents (guid, specGroup, talentId) VALUES (?, ?, ?)", pChar.Guid, specGroup, talentId);
        }

        public void RemoveTalent(Character pChar, ObjectDefines.Talent talent, bool deleteFromDb = false)
        {
            var deleted = pChar.TalentList.Remove(talent);

            if (deleted && deleteFromDb)
                DB.Characters.Execute("DELETE FROM character_talents WHERE guid = ? AND talentId = ? AND specGroup = ?", pChar.Guid, talent.Id, talent.SpecGroup);
        }

        public bool SaveSpecInfo(Character pChar)
        {
            return DB.Characters.Execute("UPDATE characters SET specGroupCount = ?, activeSpecGroup = ?, primarySpecId = ?, secondarySpecId = ? WHERE guid = ?", pChar.SpecGroupCount, pChar.ActiveSpecGroup, pChar.PrimarySpec, pChar.SecondarySpec, pChar.Guid);
        }

        #region Helpers
        public List<ObjectDefines.Talent> GetTalentsBySpecGroup(Character pChar, byte specGroup)
        {
            return pChar.TalentList.FindAll(talent => talent.SpecGroup == specGroup);
        }

        public uint GetSpecIdByGroup(Character pChar, byte specGroup)
        {
            return DBCStorage.SpecializationStorage.Where(spec => spec.Value.ClassId == pChar.Class && spec.Value.TabId == specGroup).Select(spec => spec.Value.Id).FirstOrDefault();
        }

        public byte GetSpentTalentRowCount(Character pChar, uint specGroup)
        {
            return (byte)pChar.TalentList.FindAll(talent => talent.SpecGroup == specGroup).Count;
        }

        public byte GetUnspentTalentRowCount(Character pChar)
        {
            byte maxTalentRows = GetMaxTalentRowCount(pChar);
            byte spentTalentRows = GetSpentTalentRowCount(pChar, pChar.ActiveSpecGroup);
            return (byte)(maxTalentRows - spentTalentRows);
        }

        public byte GetMaxTalentRowCount(Character pChar)
        {
            byte talentPoints = 0;
            byte[] talentLevels;

            if ((Class)pChar.Class != Class.Deathknight)
                talentLevels = new byte[] { 15, 30, 45, 60, 75, 90 };
            else
                talentLevels = new byte[] { 56, 57, 58, 60, 75, 90 };

            for (int i = 0; i < talentLevels.Length; i++)
                if (pChar.Level >= talentLevels[i])
                    talentPoints++;

            return talentPoints;
        }
        #endregion
    }
}
