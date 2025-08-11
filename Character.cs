using System.Text;

public enum Sex
{
    Male,
    Female,
    NonBinary,
}

public record struct Profession(string Label, string Employer);

public record struct Nation(string Name, string Nationality);

public record struct Demographics(
    Sex Sex,
    int Age,
    Nation Nationality,
    string Label,
    string Employer
);

public record Character(string Name, Profession Profession, Demographics Demographics)
{
    public override string ToString()
    {
        Dictionary<string, string> props = new()
        {
            { @"{name}", Name },
            { @"{label}", Demographics.Label },
        };

        StringBuilder sb = new(File.ReadAllText("data/statblock.txt"));
        string result = props.Aggregate(sb, (s, t) => s.Replace(t.Key, t.Value)).ToString();
        return result;
    }
};
