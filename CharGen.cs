using Spectre.Console;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public static class CharGen
{
    public static Character GenerateNewCharacter(
        Profession profession,
        int minAge = 25,
        int maxAge = 55,
        Sex? sexOverride = null,
        string? labelOverride = null,
        string? employerOverride = null
    )
    {
        Sex sex = sexOverride ?? GenerateSex();

        Demographics demographics = GenerateDemographics(
            sex,
            profession,
            minAge,
            maxAge,
            labelOverride,
            employerOverride
        );

        return new("Baldy", profession, demographics);
    }

    static Demographics GenerateDemographics(
        Sex sex,
        Profession profession,
        int minAge,
        int maxAge,
        string? labelOverride,
        string? employerOverride
    )
    {
        IDeserializer deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        // Load nations from YAML file
        string yamlContent = File.ReadAllText("data/nations.yaml");
        List<Nation> nations = deserializer.Deserialize<List<Nation>>(yamlContent);

        Nation nation = Random.Shared.GetItems(nations.ToArray(), 1).First();

        int age = Random.Shared.Next(minAge, maxAge + 1);
        return new Demographics(
            sex,
            age,
            nation,
            labelOverride ?? profession.Label,
            employerOverride ?? profession.Employer
        );
    }

    static Sex GenerateSex()
    {
        int value = Random.Shared.Next(0, 100);
        if (value < 45)
        {
            return Sex.Male;
        }
        else if (value < 90)
        {
            return Sex.Female;
        }
        else
        {
            return Sex.NonBinary;
        }
    }
}
