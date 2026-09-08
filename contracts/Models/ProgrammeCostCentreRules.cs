namespace Jewel.JPMS.Models;

// The trade vocabulary that maps a programme task's title to cost centres on the valuation
// report — the first, deterministic pass of a draft programme update (decision 2026-09-08:
// rules first, Claude for what they miss, the reviewer confirms, the confirmation is saved on
// the task). A task title is "where — what" ("Second Floor — Plumbing 2nd Fix"); the where
// carries nothing (the report is not split by location), the what is a trade word this
// rulebook knows. Each rule names every code in the family the word could mean; the match is
// then narrowed to the codes that actually carry priced lines on the claim, so "Window
// Installation" lands on WDR-ALU alone when aluminium is the only window line priced.
//
// Codes are the cost-centre master's, spelled exactly (list_cost_codes). Keep the phrases
// lower-case and plain: matching is on whole words after punctuation is stripped, and a longer
// phrase beats any shorter phrase it contains ("carpentry 2nd fix" silences "carpentry").
public static class ProgrammeCostCentreRules
{
    public sealed record Rule(IReadOnlyList<string> Phrases, IReadOnlyList<string> CostCodes);

    private static Rule Map(string[] phrases, params string[] costCodes) => new(phrases, costCodes);

    public static IReadOnlyList<Rule> All { get; } = new List<Rule>
    {
        // ---- Preliminaries and enabling ---------------------------------------------------
        Map(new[] { "site set up", "site setup", "site establishment", "mobilisation", "welfare", "hoarding" },
            "PRELIMS-SET", "PRELIMS-HRD", "PRELIMS-WEL", "PRELIMS-WC", "PRELIMS-SEC"),
        Map(new[] { "prelims", "preliminaries" },
            "PRELIMS-SET", "PRELIMS-HRD", "PRELIMS-WEL", "PRELIMS-WC", "PRELIMS-SEC", "PRELIMS-PRO",
            "PRELIMS-WPR", "PRELIMS-HSO", "PRELIMS-HSC", "PRELIMS-SMG", "PRELIMS-PMG", "PRELIMS-LAB", "PRELIMS-TMP"),
        Map(new[] { "site supervision", "site manager", "site management", "supervision" }, "PRELIMS-SMG"),
        Map(new[] { "project management", "project manager" }, "PRELIMS-PMG"),
        Map(new[] { "protection", "weather protection" }, "PRELIMS-PRO", "PRELIMS-WPR"),
        Map(new[] { "health and safety", "health safety", "h s", "cdm" }, "PRELIMS-HSO", "PRELIMS-HSC"),
        Map(new[] { "general labour", "labour", "labourer" }, "PRELIMS-LAB"),
        Map(new[] { "temporary works", "propping", "needling", "structural support" }, "PRELIMS-TMP", "ENABLE-STS"),
        Map(new[] { "demolition", "demolish", "strip out", "soft strip", "site clearance", "clearance", "tree removal", "tree", "trees" }, "ENABLE-DEM"),
        Map(new[] { "asbestos" }, "ENABLE-ASB"),
        Map(new[] { "core drilling", "core drill", "diamond drilling" }, "ENABLE-COR"),
        Map(new[] { "skips", "skip", "rubbish", "waste removal", "muck away", "grab" }, "ENABLE-SKP", "ENABLE-GRB"),
        Map(new[] { "scaffold", "scaffolding" }, "SCAFF-STD"),

        // ---- Substructure and structure ---------------------------------------------------
        Map(new[] { "excavation", "excavate", "reduce level", "reduced level", "dig out" }, "SUB-EXC", "SUB-GWK"),
        Map(new[] { "groundworks", "ground works", "foundations", "footings", "oversite", "ground floor slab", "beam and block", "beam block", "hardcore" }, "SUB-GWK", "SUB-CON"),
        Map(new[] { "concrete", "concreting" }, "SUB-CON"),
        Map(new[] { "below ground drainage", "underground drainage", "drainage below ground" }, "SUB-DRN"),
        Map(new[] { "above ground drainage", "internal drainage", "soil and vent", "svp" }, "MEC-DRN"),
        Map(new[] { "drainage", "drains", "manhole", "soakaway" }, "SUB-DRN", "MEC-DRN"),
        Map(new[] { "piling", "piles" }, "SUB-PIL"),
        Map(new[] { "underpinning", "under pinning", "underpin" }, "SUB-UND"),
        Map(new[] { "structural steel", "steelwork", "steel work", "steels", "steel", "rsj", "rsjs" }, "STR-STL", "STR-DSL"),
        Map(new[] { "mesh" }, "STR-MSH"),
        Map(new[] { "masonry", "brickwork", "brick work", "blockwork", "block work", "bricklaying", "brickie", "superstructure", "walls up" }, "MASON-BRK"),
        Map(new[] { "stonework", "stone work", "stone masonry" }, "MASON-STN"),
        Map(new[] { "stone cladding" }, "EXT-STC"),
        Map(new[] { "timber cladding" }, "EXT-TIC"),
        Map(new[] { "cladding" }, "EXT-STC", "EXT-TIC"),
        Map(new[] { "copings", "coping", "cappings", "capping" }, "EXT-STC-COP", "EXT-MCP"),

        // ---- Roof --------------------------------------------------------------------------
        Map(new[] { "roof carpentry", "cut roof", "roof structure", "rafters", "roof timbers" }, "CARP-CUT", "CARP-TRS"),
        Map(new[] { "trusses", "truss", "trussed roof" }, "CARP-TRS"),
        Map(new[] { "flat roof", "membrane", "grp roof", "single ply" }, "ROOF-FLT"),
        Map(new[] { "leadwork", "lead work", "lead flashing", "lead flashings" }, "ROOF-LED"),
        Map(new[] { "fascia", "fascias", "soffit", "soffits" }, "ROOF-FSU", "ROOF-FSM"),
        Map(new[] { "gutters", "gutter", "rainwater", "downpipes", "downpipe" }, "ROOF-GRU", "ROOF-GRM"),
        Map(new[] { "roof tiles", "roof tiling", "retile", "re tile", "slates", "slating" }, "ROOF-TLN", "ROOF-TLO", "ROOF-RFR"),
        Map(new[] { "roof", "roofing", "roofer", "roofs" }, "ROOF-RFR", "ROOF-TLN", "ROOF-TLO", "ROOF-FLT", "ROOF-LED"),

        // ---- Carpentry and joinery ---------------------------------------------------------
        Map(new[] { "carpentry 1st fix", "carpentry first fix", "1st fix carpentry", "first fix carpentry", "1st fix carpenter" }, "CARP-1FX"),
        Map(new[] { "carpentry 2nd fix", "carpentry second fix", "2nd fix carpentry", "second fix carpentry", "2nd fix carpenter" }, "CARP-2FX"),
        Map(new[] { "carpentry", "carpenter", "carpenters" }, "CARP-1FX", "CARP-2FX"),
        Map(new[] { "joinery", "bespoke joinery", "fitted furniture" }, "CARP-JNR"),
        Map(new[] { "wardrobes", "wardrobe" }, "CARP-WRD"),
        Map(new[] { "kitchen", "kitchens", "kitchen fit", "kitchen installation" }, "CARP-KIT", "SUP-KIT"),
        Map(new[] { "appliances", "appliance" }, "SUP-APP"),
        Map(new[] { "internal doors", "internal door", "door linings", "doors and linings" }, "CARP-DOR", "SUP-DOR"),
        Map(new[] { "ironmongery" }, "SUP-IRO"),
        Map(new[] { "furniture" }, "SUP-FRN"),

        // ---- Stairs, balustrades, windows and doors ---------------------------------------
        Map(new[] { "staircase", "staircases", "stairs", "stair", "stairway" }, "STAIR-MTL", "STAIR-TIM", "STAIR-GLS"),
        Map(new[] { "balustrade", "balustrades", "balustrading", "railings", "railing", "handrail", "handrails", "juliet" }, "STR-MRL", "STR-GRL"),
        Map(new[] { "garage door", "garage doors" }, "WDR-GAR"),
        Map(new[] { "glazed screen", "glazed screens", "curtain wall", "curtain walling", "structural glazing", "specialist glazing" }, "WDR-SPG"),
        Map(new[] { "crittall", "internal glazing", "internal glazed" }, "WDR-INT"),
        Map(new[] { "windows", "window", "glazing", "bifold", "bifolds", "bi fold", "bi folds", "sliding doors", "sliding door", "rooflight", "rooflights", "roof light", "roof lights", "skylight", "skylights", "external doors", "external door", "entrance door", "front door" },
            "WDR-TIM", "WDR-UPV", "WDR-ALU"),

        // ---- Internal finishes -------------------------------------------------------------
        Map(new[] { "insulation", "insulate", "insulating" }, "INT-INW", "INT-INC", "INT-INF"),
        Map(new[] { "dry lining", "drylining", "dry line" }, "INT-PLB", "INT-PDD", "INT-MGW"),
        Map(new[] { "plasterboard", "plasterboarding", "plaster boarding", "boarding", "dot and dab" }, "INT-PLB", "INT-PDD"),
        Map(new[] { "partitions", "partition", "stud walls", "stud wall", "metal stud", "gypliner" }, "INT-MGW"),
        Map(new[] { "mf ceiling", "mf ceilings", "suspended ceiling", "suspended ceilings" }, "INT-MFC"),
        Map(new[] { "spray plaster" }, "INT-SPR"),
        Map(new[] { "plastering", "plaster", "skim", "skimming" }, "INT-PLS"),
        Map(new[] { "render", "rendering", "rendered" }, "INT-RDR"),
        Map(new[] { "coving", "cornice", "cornicing" }, "INT-COV"),
        Map(new[] { "screed", "screeding" }, "FLR-SCR"),
        Map(new[] { "self levelling", "latex", "levelling compound" }, "FLR-SLF"),
        Map(new[] { "carpet", "carpets" }, "FLR-CPT"),
        Map(new[] { "lvt", "vinyl", "lino", "linoleum" }, "FLR-LVT"),
        Map(new[] { "timber flooring", "timber floor", "wood flooring", "wood floor", "engineered flooring", "engineered floor", "parquet", "herringbone" }, "FLR-WD"),
        Map(new[] { "flooring", "floor finishes", "floor finish", "floor coverings" }, "FLR-WD", "FLR-CPT", "FLR-LVT"),
        Map(new[] { "marble" }, "TIL-MRB"),
        Map(new[] { "stone tiling", "stone tiles", "stone floor" }, "TIL-STN"),
        Map(new[] { "tiling", "tiles", "tile", "tiler", "tiled" }, "TIL-STD", "TIL-STN", "TIL-MRB"),
        Map(new[] { "tanking", "waterproofing" }, "WPF-INT", "WPF-EXT"),
        Map(new[] { "damp proofing", "damp proof", "dpc" }, "WPF-DMP"),
        Map(new[] { "wallpaper", "wallpapering" }, "DEC-WLP"),
        Map(new[] { "decoration", "decorations", "decorating", "decorator", "decorators", "painting", "paint", "redecoration", "mist coat" }, "DEC-STD"),
        Map(new[] { "fireplace", "fireplaces", "stove", "stoves", "chimney", "fire surround" }, "DEC-FIR"),
        Map(new[] { "fire stopping", "firestopping" }, "FIRE-STP"),
        Map(new[] { "passive fire", "fire protection", "intumescent" }, "FIRE-PSV"),

        // ---- Mechanical --------------------------------------------------------------------
        Map(new[] { "underfloor heating", "under floor heating", "ufh" }, "MEC-UFH"),
        Map(new[] { "heat pump", "heat pumps", "heat source", "ashp", "gshp" }, "MEC-HTS"),
        Map(new[] { "boiler", "boilers" }, "MEC-BLR", "MEC-FUL"),
        Map(new[] { "heating" }, "MEC-PLM", "MEC-HTS", "MEC-BLR", "MEC-UFH"),
        Map(new[] { "air conditioning", "air con", "aircon", "a c", "vrf", "comfort cooling" }, "MEC-AC"),
        Map(new[] { "ventilation", "mvhr", "extract", "extraction" }, "MEC-VNT"),
        Map(new[] { "solar", "pv", "photovoltaic" }, "MEC-SOL"),
        Map(new[] { "specialist plumbing" }, "MEC-PLS"),
        Map(new[] { "plumbing", "plumber", "plumbers", "plumb", "sanitaryware", "sanitary ware", "sanitary", "bathroom fit", "bathrooms", "bathroom" }, "MEC-PLM"),

        // ---- Electrical --------------------------------------------------------------------
        Map(new[] { "ev charger", "ev chargers", "ev charging", "car charger" }, "ELE-EVC"),
        Map(new[] { "audio visual", "av", "sound system", "cinema", "home cinema", "smart home", "home automation" }, "ELE-AV"),
        Map(new[] { "cctv" }, "ELE-CCT"),
        Map(new[] { "entry system", "entry systems", "intercom", "video entry", "access control" }, "ELE-ENT"),
        Map(new[] { "fire alarm", "fire alarms", "smoke alarm", "smoke alarms", "smoke detectors" }, "ELE-FIR"),
        Map(new[] { "alarm", "alarms", "security system", "intruder" }, "ELE-ALM"),
        Map(new[] { "specialist electrical", "specialist electrician" }, "ELE-SPE"),
        Map(new[] { "electrics", "electrical", "electrician", "electricians", "electric", "wiring", "rewire", "lighting" }, "ELE-STD"),

        // ---- External works and utilities --------------------------------------------------
        Map(new[] { "landscaping", "landscape", "landscaper", "garden", "gardens", "planting" }, "EXTW-LND"),
        Map(new[] { "paving", "patio", "patios", "driveway", "drive", "paths", "pathway", "tarmac", "resin", "block paving" }, "EXTW-PAV"),
        Map(new[] { "fencing", "fence", "fences", "boundary", "boundaries", "gates", "gate" }, "EXTW-FEN"),
        Map(new[] { "turfing", "turf", "lawn", "lawns" }, "EXTW-TRF"),
        Map(new[] { "decking" }, "EXTW-DEK"),
        Map(new[] { "bbq", "outdoor kitchen" }, "EXTW-BBQ"),
        Map(new[] { "shed", "sheds", "outbuilding", "outbuildings", "out house", "garden room", "summer house", "summerhouse" }, "EXTW-SHD"),
        Map(new[] { "gazebo", "carport", "car port", "pergola", "awning" }, "SPEC-GAZ"),
        Map(new[] { "swimming pool", "pool" }, "SPEC-POO"),
        Map(new[] { "spa", "hot tub", "sauna", "steam room" }, "SPEC-SPA"),
        Map(new[] { "lift", "lifts", "hoist", "platform lift" }, "SPEC-LFT"),
        Map(new[] { "trenching", "trench", "trenches", "service trench", "bore hole", "borehole" }, "UTIL-TRN"),
        Map(new[] { "utilities", "utility", "services connection", "service connections", "water connection", "gas connection", "electricity connection", "meter", "meters" }, "UTIL-STD", "UTIL-TRN"),
        Map(new[] { "blinds", "curtains" }, "WIN-BLD"),

        // ---- Handover ----------------------------------------------------------------------
        Map(new[] { "builders clean", "sparkle clean", "final clean", "cleaning", "clean" }, "HAND-CLI", "HAND-CLE"),
    }.AsReadOnly();

    // The codes a task title matches among those the claim actually prices, in rulebook order.
    public static IReadOnlyList<string> Match(string title, IReadOnlyCollection<string> presentCostCodes)
    {
        var text = Normalise(title);
        if (text.Length == 0 || presentCostCodes.Count == 0) return Array.Empty<string>();

        var matches = All
            .SelectMany(rule => rule.Phrases
                .Where(phrase => ContainsWholePhrase(text, phrase))
                .Select(phrase => (Rule: rule, Phrase: phrase)))
            .ToList();

        // A longer phrase silences any shorter phrase it contains, whichever rule it belongs to:
        // "carpentry 2nd fix" is a different, more precise statement than "carpentry".
        var surviving = matches
            .Where(match => !matches.Any(other =>
                !ReferenceEquals(other.Rule, match.Rule)
                && other.Phrase.Length > match.Phrase.Length
                && ContainsWholePhrase(Normalise(other.Phrase), match.Phrase)))
            .Select(match => match.Rule)
            .Distinct()
            .ToList();

        var present = new HashSet<string>(presentCostCodes, StringComparer.OrdinalIgnoreCase);
        return surviving
            .SelectMany(rule => rule.CostCodes)
            .Where(present.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }

    // Lower-case words separated by single spaces; every punctuation mark becomes a space so
    // "Insulation/Dry Lining/Plaster" and "Tree & Fence Removal" split into their words.
    public static string Normalise(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var characters = text.ToLowerInvariant()
            .Select(character => char.IsLetterOrDigit(character) ? character : ' ')
            .ToArray();
        return string.Join(' ', new string(characters).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static bool ContainsWholePhrase(string normalisedText, string phrase) =>
        $" {normalisedText} ".Contains($" {Normalise(phrase)} ", StringComparison.Ordinal);
}
