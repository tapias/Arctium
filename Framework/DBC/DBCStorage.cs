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

using System.Collections.Generic;

namespace Framework.DBC
{
    public static class DBCStorage
    {
        internal static int DBCFileCount = 0;

        public static Dictionary<uint, ChrClasses> ClassStorage;
        public static Dictionary<uint, ChrRaces> RaceStorage;
        public static Dictionary<uint, CharStartOutfit> CharStartOutfitStorage;
        public static Dictionary<uint, NameGen> NameGenStorage;
        public static Dictionary<uint, ChrSpecialization> SpecializationStorage;
        public static Dictionary<uint, SpecializationSpell> SpecializationSpellStorage;
        public static Dictionary<uint, Spell> SpellStorage;
        public static Dictionary<uint, SpellLevels> SpellLevelStorage;
        public static Dictionary<uint, Talent> TalentStorage;

        //Strings
        internal static Dictionary<uint, string> ClassStrings = new Dictionary<uint, string>();
        internal static Dictionary<uint, string> RaceStrings = new Dictionary<uint, string>();
        internal static Dictionary<uint, string> NameGenStrings = new Dictionary<uint, string>();
    }
}
