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

using System.Runtime.InteropServices;

namespace Framework.DBC
{
    public struct DbcHeader
    {
        public int Signature;
        public int RecordsCount;
        public int FieldsCount;
        public int RecordSize;
        public int StringTableSize;

        public bool IsDBC
        {
            get { return Signature == 0x43424457; }
        }

        public long DataSize
        {
            get { return (long)(RecordsCount * RecordSize); }
        }

        public long StartStringPosition
        {
            get { return DataSize + (long)Marshal.SizeOf(typeof(DbcHeader)); }
        }
    };


    public struct ChrClasses
    {
        public uint ClassID;                                    // 0        m_ID
        public uint powerType;                                  // 1        m_DisplayPower
        // 2        m_petNameToken
        public uint _name;                                      // 3        m_name_lang
        //char*       nameFemale;                               // 4        m_name_female_lang
        //char*       nameNeutralGender;                        // 5        m_name_male_lang
        //char*       capitalizedName                           // 6,       m_filename
        public uint spellfamily;                                // 7        m_spellClassSet
        //uint32 flags2;                                        // 8        m_flags (0x08 HasRelicSlot)
        public uint CinematicSequence;                          // 9        m_cinematicSequenceID
        public uint expansion;                                  // 10       m_required_expansion
        //uint32                                                // 11
        //uint32                                                // 12
        //uint32                                                // 13

        /// <summary>
        /// Return current Race Name
        /// </summary>
        public string ClassName
        {
            get { return DBCStorage.ClassStrings.LookupByKey(_name); }
        }
    };

    public struct ChrRaces
    {
        public uint RaceID;                                     // 0
        // 1 unused
        public uint FactionID;                                  // 2 faction template id
        // 3 unused
        public uint model_m;                                    // 4
        public uint model_f;                                    // 5
        // 6 unused
        public uint TeamID;                                     // 7 (7-Alliance 1-Horde)
        // 8-11 unused
        public uint CinematicSequence;                          // 12 id from CinematicSequences.dbc
        // 13 unused
        public uint _name;                                      // 14
        // 17
        // 16 
        // 17-18    m_facialHairCustomization[2]
        // 19       m_hairCustomization
        //uint32                                                // 20 (23 for worgens)
        //uint32                                                // 21 4.0.0
        //uint32                                                // 22 4.0.0

        /// <summary>
        /// Return current Race Name
        /// </summary>
        public string RaceName
        {
            get { return DBCStorage.RaceStrings.LookupByKey(_name); }
        }
    };

    public struct ChrSpecialization
    {
        public uint Id;                                         // 0
        //string  Icon                                          // 1
        public uint ClassId;                                    // 2    0 = Pet
        public uint MasterySpellId;                             // 3
        //uint32  unknown                                       // 4    all 0
        public uint TabId;                                      // 5    0 / 1 / 2 / 3
        //uint32  unknown                                       // 6    only for Pets values 0, 1, 2
        public uint RoleTypeId;                                 // 7    0 = Tank / 1 = Heal / 2 = Damage
        //uint32                                                // 8
        //uint32                                                // 9
        //uint32                                                // 10
        //string  Name;                                         // 11
        //string  Description;                                  // 12
        //uint32                                                // 13
    };

    public struct CharStartOutfit
    {
        public uint Mask;      // Race, Class, Gender, ?

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public uint[] ItemId;
    };

    public struct NameGen
    {
        public uint Id;
        public uint _name;
        public uint Race;
        public uint Gender;

        public string Name
        {
            get { return DBCStorage.NameGenStrings.LookupByKey(_name); }
        }
    };

    public struct SpecializationSpell
    {
        //uint32                                                // 0
        public uint SpecId;                                     // 1    value is taken from ChrSpecialization.Id
        public uint SpellId;                                    // 2
        public uint SwitchSpellId;                              // 3
        //uint                                                  // 4    all 0
    };

    public struct Spell
    {
        public uint Id;                                         // 0        m_ID
        //string SpellName;                                     // 1        m_name_lang
        //string SubText;                                       // 2        m_nameSubtext_lang
        //string Description;                                   // 3        m_description_lang not used
        //string AuraDescription;                               // 4        m_auraDescription_lang not used
        public uint RuneCostId;                                 // 5        m_runeCostID
        //public uint SpellMissileId;                           // 6        m_spellMissileID not used
        //public uint SpellDescriptionVariableId;               // 7        m_spellDescriptionVariableID, 3.2.0
        //float   unk_f1;                                       // 8
        public uint SpellScalingId;                             // 9        SpellScaling.dbc
        public uint SpellAuraOptionsId;                         // 10       SpellAuraOptions.dbc
        public uint SpellAuraRestrictionsId;                    // 11       SpellAuraRestrictions.dbc
        public uint SpellCastingRequirementsId;                 // 12       SpellCastingRequirements.dbc
        public uint SpellCategoriesId;                          // 13       SpellCategories.dbc
        public uint SpellClassOptionsId;                        // 14       SpellClassOptions.dbc
        public uint SpellCooldownsId;                           // 15       SpellCooldowns.dbc
        public uint SpellEquippedItemsId;                       // 16       SpellEquippedItems.dbc
        public uint SpellInterruptsId;                          // 17       SpellInterrupts.dbc
        public uint SpellLevelsId;                              // 18       SpellLevels.dbc
        public uint SpellReagentsId;                            // 19       SpellReagents.dbc
        public uint SpellShapeshiftId;                          // 20       SpellShapeshift.dbc
        public uint SpellTargetRestrictionsId;                  // 21       SpellTargetRestrictions.dbc
        public uint SpellTotemsId;                              // 22       SpellTotems.dbc
        public uint ResearchProjectId;                          // 23
        public uint SpellMiscId;                                // 24       SpellMisc.dbc
    };

    public struct SpellLevels
    {
        public uint Id;                                         // 0
        public uint SpellId;                                    // 1
        //uint32                                                // 2
        public uint BaseLevel;                                  // 3
        public uint MaxLevel;                                   // 4
        public uint SpellLevel;                                 // 5
    };

    public struct Talent
    {
        public uint Id;                                         // 0
        //uint32                                                // 1        (pet talent related)
        public uint Row;                                        // 2
        public uint Column;                                     // 3
        public uint SpellId;                                    // 4
        //uint32                                                // 5        (pet talent related)
        //uint32                                                // 6        (pet talent related)
        //int32                                                 // 7        (pet talent related)
        public uint ClassId;                                    // 8        (class id 0 are pets)
        public uint ReplaceSpellId;                             // 9
        //uint32                                                // 10       unknown
    };
}
