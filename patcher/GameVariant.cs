// A game "variant" = a specific localised / fan-patched build of Metal Fatigue that the patcher
// must adapt to. The only axis that matters today is the build-menu icon set in
// TBD\structures{X,Y,Z}.tbd: localisation patches reorder or drop icons, so a fixed catalogue
// index (PartsData.IconIndex, which is the ENGLISH ordering) points at the wrong picture. This
// class maps the canonical English index to each variant's actual file index.
//
// Design goals (2026-07-24):
//   * table-driven and extensible — a new variant is ONE entry in Known, nothing else;
//   * many behaviours per variant, not just a single offset (icon removals + shift, UI language,
//     display name), so odd cases in other language patches can be expressed;
//   * self-validating detection — the icon CARD colour encodes the slot (Arm=red, Torso=blue,
//     Legs=green) independently of language, so a wrong/unknown variant fails safe to the text
//     tree instead of silently showing wrong icons.
//
// Verified 2026-07-24 against both reference installs (Steam=English, GOG=German fan patch),
// decoding both with the shipped GameIcons: English 77 icons / identity = 48/48 parts correct;
// German 74 icons (the 3 alien icons removed) / rule below = 46 correct, 0 wrong, 2 absent (the
// Hedoth alien parts, correctly iconless in German).
using System.Collections.Generic;

namespace MetalFatiguePatcher
{
    internal sealed class GameVariant
    {
        public string Key;                    // stable id, also the issue-report tag
        public string NameKey;                // Lang key for the human-readable name
        public Lang.L UiLanguage;             // patcher language this variant suggests
        public int[] RemovedIconIndices;      // canonical (English) icon indices absent here
        public int IconIndexShift;            // extra uniform shift after removals (rare)

        /// <summary>
        /// Map a canonical (English catalogue) icon index to this variant's file index, or -1 if
        /// the icon does not exist in this variant (e.g. the alien icons a localisation dropped).
        /// </summary>
        public int MapIcon(int canonical)
        {
            if (canonical < 0) return -1;
            if (RemovedIconIndices != null)
            {
                int removedBefore = 0;
                foreach (var r in RemovedIconIndices)
                {
                    if (r == canonical) return -1;
                    if (r < canonical) removedBefore++;
                }
                return canonical - removedBefore + IconIndexShift;
            }
            return canonical + IconIndexShift;
        }

        // --- known variants (add one entry to support a new build) --------------------------
        public static readonly GameVariant English = new GameVariant
        {
            Key = "english", NameKey = "variant.english", UiLanguage = Lang.L.EN,
            RemovedIconIndices = null, IconIndexShift = 0,
        };

        // German fan patch (SETUP_Metal_Fatigue_Deutschpatch): drops the 3 alien build icons
        // (canonical 3/4/5) and thus shifts everything from 6 up down by three.
        public static readonly GameVariant GermanFanPatch = new GameVariant
        {
            Key = "german-fanpatch", NameKey = "variant.german", UiLanguage = Lang.L.DE,
            RemovedIconIndices = new[] { 3, 4, 5 }, IconIndexShift = 0,
        };

        // English is tried first so a clean install always wins ties.
        public static readonly GameVariant[] Known = { English, GermanFanPatch };

        // ---------------------------------------------------------------------------------------
        // Detection: score every known variant against the decoded icons using the language-neutral
        // slot-colour check, and take the best clean match.

        public sealed class Match
        {
            public GameVariant Variant;   // null = no known variant fits (unknown build)
            public int Ok, Bad, Absent;   // slot-colour validation tally for the winner
            public int[] FactionIconCounts = new int[3];   // Rimtech / MilAgro / Neuropa decoded counts
        }

        static readonly string[] CountFactions = { "Rimtech", "MilAgro", "Neuropa" };

        /// <summary>
        /// Pick the variant whose icon mapping validates cleanly (zero slot-colour mismatches) with
        /// the most confirmed parts. Returns a Match whose Variant is null when nothing fits, so the
        /// caller can drop to the text tree and offer a report link.
        /// </summary>
        public static Match Detect(GameIcons icons)
        {
            var result = new Match();
            for (int i = 0; i < CountFactions.Length; i++)
            {
                var l = icons.Faction(CountFactions[i]);
                result.FactionIconCounts[i] = l?.Count ?? 0;
            }

            Match best = null;
            foreach (var v in Known)
            {
                int ok = 0, bad = 0, absent = 0;
                foreach (var p in PartsData.Parts)
                {
                    var fac = p.Faction == "Hedoth" ? "MilAgro" : p.Faction;   // aliens borrow MilAgro's file
                    var list = icons.Faction(fac);
                    if (list == null) continue;
                    int fi = v.MapIcon(p.IconIndex);
                    if (fi < 0) { absent++; continue; }
                    if (fi >= list.Count) { bad++; continue; }
                    var slot = GameIcons.ClassifySlotColor(list[fi]);
                    if (slot == null) { bad++; continue; }
                    if (slot == p.Slot) ok++; else bad++;
                }
                // A clean match has zero mismatches; among those prefer the most confirmations.
                if (bad == 0 && (best == null || ok > best.Ok))
                {
                    best = new Match { Variant = v, Ok = ok, Bad = bad, Absent = absent,
                                       FactionIconCounts = result.FactionIconCounts };
                }
            }
            return best ?? result;   // best (clean match) or a Variant==null result carrying the counts
        }
    }
}
