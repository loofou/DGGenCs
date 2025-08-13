using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

public enum Sex
{
    Male,
    Female,
    NonBinary,
}

public enum CharacterType
{
    Agent,
    NPC,
}

public enum NPCType
{
    Child,
    Youth,
    Novice,
    Ordinary,
    Expert,
}

public record struct ProfessionSkillPack(
    Dictionary<string, int> Always,
    Dictionary<string, int> Pick,
    int PickAmount = 0
);

public record struct ProfessionNPCConfig(List<string> ImportantStats, List<string> ImportantSkills);

public record struct ProfessionSpecialTraining(int Chance, List<string> Trainings);

public record struct Profession(
    string? Override,
    string Label,
    string Employer,
    string Division,
    ProfessionSkillPack Skills,
    int Bonds,
    ProfessionNPCConfig Npc,
    ProfessionSpecialTraining SpecialTraining,
    string GearKit
);

public record struct Motivation(int chances, List<string> motivation, List<string> objects);

public record struct Nation(string Name, string Nationality, string NativeLanguage);

public record struct Demographics(
    Sex Sex,
    int Age,
    DateTime BirthDay,
    Nation Nationality,
    string Label,
    string Employer
);

public record struct Statistics(
    int Strength,
    int Constitution,
    int Dexterity,
    int Intelligence,
    int Power,
    int Charisma
)
{
    public readonly int this[string stat] =>
        stat.ToUpperInvariant() switch
        {
            "STR" => Strength,
            "CON" => Constitution,
            "DEX" => Dexterity,
            "INT" => Intelligence,
            "POW" => Power,
            "CHA" => Charisma,
            _ => throw new ArgumentException($"Unknown statistic: {stat}"),
        };

    public override readonly string ToString() =>
        $"STR {Strength} CON {Constitution} DEX {Dexterity} INT {Intelligence} POW {Power} CHA {Charisma}";
};

public record struct DerivedStatistics(int HP, int WP, int SAN, int BreakingPoint)
{
    public override string ToString() =>
        $"HP {HP} WP {WP} SAN {SAN} BREAKING POINT {BreakingPoint}";
};

public record struct Gear(
    Dictionary<string, Weapon> Weapons,
    Dictionary<string, Armor> Armor,
    Dictionary<string, string> Other
);

public record struct Weapon(
    string Name,
    string Skill = "",
    string Stat = "",
    string Damage = "",
    int ArmorPiercing = 0,
    int Lethality = 0
);

public record struct Armor(string Name, int ArmorRating);

public record struct GearItem(string Item, int? Chance = null);

public record struct GearKit(List<GearItem> Weapons, List<GearItem> Armor, List<GearItem> Other);

public partial record Character(
    string Name,
    Profession Profession,
    Demographics Demographics,
    Statistics Stats,
    DerivedStatistics DerivedStats,
    Dictionary<string, int> Skills,
    List<string> Bonds,
    List<string> SpecialTraining,
    List<Weapon> Attacks,
    List<Armor> Armor,
    List<string> Equipment,
    List<string> MotivationsDisorders
)
{
    [GeneratedRegex(@"(\r?\n){3,}")]
    private static partial Regex EmptylineRegex();

    public override string ToString()
    {
        TextInfo ti = CultureInfo.InvariantCulture.TextInfo;

        string attacks = string.Join(
            "\n",
            Attacks.Select(a =>
                $"{a.Name} {(a.Skill != "" ? Skills[a.Skill] : Stats[a.Stat] * 5)}%"
                + $"{(a.Damage != "" ? $", Damage {a.Damage}" : string.Empty)}"
                + $"{(a.Lethality > 0 ? $", Lethality {a.Lethality}%" : string.Empty)}"
                + $"{(a.ArmorPiercing > 0 ? $", Armor Piercing {a.ArmorPiercing}" : string.Empty)}."
            )
        );

        Dictionary<string, string> props = new()
        {
            { @"{name}", Name },
            { @"{label}", Demographics.Label },
            {
                @"{sex}",
                Demographics.Sex == Sex.Male ? "M"
                : Demographics.Sex == Sex.Female ? "F"
                : "NB"
            },
            { @"{age}", $"{Demographics.Age} ({Demographics.BirthDay:MMM dd})" },
            { @"{statistics}", Stats.ToString() },
            { @"{derived_statistics}", DerivedStats.ToString() },
            {
                @"{skills}",
                $"SKILLS: {string.Join(", ", Skills.Select(s => $"{ti.ToTitleCase(s.Key)} {s.Value}%").Order())}"
            },
            {
                @"{special_training}",
                SpecialTraining.Count > 0
                    ? $"SPECIAL TRAINING: {string.Join(", ", SpecialTraining.Select(s => ti.ToTitleCase(s)))}"
                    : ""
            },
            {
                @"{bonds}",
                $"BONDS: {string.Join("\n", Bonds.Select(b => $"{b}, {Stats.Charisma}."))}"
            },
            {
                @"{motivations_disorders}",
                $"MOTIVATIONS AND DISORDERS: {string.Join("\n", MotivationsDisorders)}"
            },
            {
                @"{armor}",
                Armor.Count > 0
                    ? $"ARMOR: {string.Join("\n", Armor.Select(a => $"{a.Name} (Armor {a.ArmorRating})."))}"
                    : ""
            },
            { @"{attacks}", Attacks.Count > 0 ? $"ATTACKS: {attacks}" : string.Empty },
            {
                @"{equipment}",
                Equipment.Count > 0 ? $"EQUIPMENT: {string.Join(", ", Equipment)}" : string.Empty
            },
            {
                @"{notes}",
                $"{(Demographics.Employer != "" ? $"EMPLOYER: {Demographics.Employer}, " : string.Empty)}NATIONALITY: {Demographics.Nationality.Nationality} ({Demographics.Nationality.Name})"
            },
        };

        StringBuilder sb = new(File.ReadAllText("data/statblock.txt"));
        string result = props.Aggregate(sb, (s, t) => s.Replace(t.Key, t.Value)).ToString();

        //Remove all consecutive empty lines and only leave a single empty line
        result = EmptylineRegex().Replace(result, Environment.NewLine + Environment.NewLine);
        return result;
    }
};
