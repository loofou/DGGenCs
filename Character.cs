using System.Collections.Immutable;
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

public record struct ProfessionNPCConfig(List<string> ImportantStats, List<String> ImportantSkills);

public record struct Profession(
    string Label,
    string Employer,
    string Division,
    ProfessionSkillPack Skills,
    int Bonds,
    ProfessionNPCConfig Npc
);

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
    public override string ToString() =>
        $"STR {Strength} CON {Constitution} DEX {Dexterity} INT {Intelligence} POW {Power} CHA {Charisma}";
};

public record struct DerivedStatistics(int HP, int WP, int SAN, int BreakingPoint)
{
    public override string ToString() =>
        $"HP {HP} WP {WP} SAN {SAN} BREAKING POINT {BreakingPoint}";
};

public partial record Character(
    string Name,
    Profession Profession,
    Demographics Demographics,
    Statistics Stats,
    DerivedStatistics DerivedStats,
    Dictionary<string, int> Skills,
    List<string> Bonds
)
{
    [GeneratedRegex(@"(\r?\n){3,}")]
    private static partial Regex EmptylineRegex();

    public override string ToString()
    {
        TextInfo ti = CultureInfo.InvariantCulture.TextInfo;

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
            { @"{special_training}", "" },
            { @"{bonds}", $"BONDS: {string.Join(", ", Bonds)}" },
            { @"{motivations_disorders}", "" },
            { @"{armor}", "" },
            { @"{attacks}", "" },
            { @"{equipment}", "" },
            {
                @"{notes}",
                $"EMPLOYER: {Demographics.Employer}, NATIONALITY: {Demographics.Nationality.Nationality} ({Demographics.Nationality.Name})"
            },
        };

        StringBuilder sb = new(File.ReadAllText("data/statblock.txt"));
        string result = props.Aggregate(sb, (s, t) => s.Replace(t.Key, t.Value)).ToString();

        //Remove all consecutive empty lines and only leave a single empty line
        result = EmptylineRegex().Replace(result, Environment.NewLine + Environment.NewLine);
        return result;
    }
};
