using System.ComponentModel;
using Spectre.Console;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public static class CharGen
{
    static readonly List<int[]> StatPools =
    [
        [13, 13, 12, 12, 11, 11],
        [15, 14, 12, 11, 10, 10],
        [17, 14, 13, 10, 10, 8],
    ];

    static readonly Dictionary<string, int> DefaultSkills = new Dictionary<string, int>
    {
        { "accounting", 10 },
        { "alertness", 20 },
        { "athletics", 30 },
        { "bureaucracy", 10 },
        { "criminology", 10 },
        { "disguise", 10 },
        { "dodge", 30 },
        { "drive", 20 },
        { "firearms", 20 },
        { "first aid", 10 },
        { "heavy machinery", 10 },
        { "history", 10 },
        { "humint", 10 },
        { "melee weapons", 30 },
        { "navigate", 10 },
        { "occult", 10 },
        { "persuade", 20 },
        { "psychotherapy", 10 },
        { "ride", 10 },
        { "search", 20 },
        { "stealth", 10 },
        { "survival", 10 },
        { "swim", 20 },
        { "unarmed combat", 40 },
    };

    static readonly List<string> AllBonusSkills =
    [
        "accounting",
        "alertness",
        "anthropology",
        "archeology",
        "art1",
        "artillery",
        "athletics",
        "bureaucracy",
        "computer science",
        "craft1",
        "criminology",
        "demolitions",
        "disguise",
        "dodge",
        "drive",
        "firearms",
        "first aid",
        "forensics",
        "heavy machinery",
        "heavy weapons",
        "history",
        "HUMINT",
        "law",
        "medicine",
        "melee weapons",
        "militaryscience1",
        "navigate",
        "occult",
        "persuade",
        "pharmacy",
        "pilot1",
        "psychotherapy",
        "ride",
        "science1",
        "search",
        "SIGINT",
        "stealth",
        "surgery",
        "survival",
        "swim",
        "unarmed combat",
        "language1",
    ];

    public static Character GenerateNewCharacter(
        Profession profession,
        int minAge = 25,
        int maxAge = 55,
        Sex? sexOverride = null,
        string? labelOverride = null,
        string? employerOverride = null,
        bool randomNationality = false,
        bool verbose = false
    )
    {
        Sex sex = sexOverride ?? GenerateSex();
        if (verbose)
            Console.WriteLine(sex);

        Demographics demographics = GenerateDemographics(
            sex,
            profession,
            minAge,
            maxAge,
            labelOverride,
            employerOverride,
            randomNationality
        );
        if (verbose)
            Console.WriteLine(demographics);
        string name = GenerateName(sex);
        if (verbose)
            Console.WriteLine(name);

        Statistics stats = GenerateStats();
        if (verbose)
            Console.WriteLine(stats);
        DerivedStatistics derivedStats = GenerateDerivedStats(stats);
        if (verbose)
            Console.WriteLine(derivedStats);
        Dictionary<string, int> skills = GenerateSkills(
            profession,
            demographics.Nationality,
            verbose
        );

        return new(name, profession, demographics, stats, derivedStats, skills);
    }

    static Demographics GenerateDemographics(
        Sex sex,
        Profession profession,
        int minAge,
        int maxAge,
        string? labelOverride,
        string? employerOverride,
        bool randomNationality = false
    )
    {
        IDeserializer deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        // Load nations from YAML file
        string yamlContent = File.ReadAllText("data/nations.yaml");
        Dictionary<string, Nation> nations = deserializer.Deserialize<Dictionary<string, Nation>>(
            yamlContent
        );

        Nation nation = randomNationality
            ? Random.Shared.GetItems(nations.Values.ToArray(), 1).First()
            : nations["usa"];

        int age = Random.Shared.Next(minAge, maxAge + 1);
        DateTime birthDay = new(1995, 1, 1);
        birthDay = birthDay.AddDays(Random.Shared.Next(0, 366));

        return new Demographics(
            sex,
            age,
            birthDay,
            nation,
            labelOverride ?? profession.Label,
            employerOverride
                ?? profession.Employer
                    + (profession.Division is not null ? $" ({profession.Division})" : "")
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

    static string GenerateName(Sex sex)
    {
        string file = "data/names_male.txt";

        if (sex == Sex.Female || (sex == Sex.NonBinary && Random.Shared.Next(0, 100) < 50))
        {
            file = "data/names_female.txt";
        }

        string[] firstNames = File.ReadAllLines(file);
        string[] lastNames = File.ReadAllLines("data/surnames.txt");

        string firstName = Random.Shared.GetItems(firstNames, 1).First();
        string lastName = Random.Shared.GetItems(lastNames, 1).First();
        return $"{firstName} {lastName}";
    }

    static Statistics GenerateStats()
    {
        //Create random rolled pool of stats
        List<int[]> rolledPools = [];
        for (int i = 0; i < 3; i++)
        {
            int[] rolledPool = new int[6];
            for (int j = 0; j < rolledPool.Length; j++)
            {
                //Roll 4 d6 and drop the lowest die
                List<int> dice = new();
                for (int k = 0; k < 4; k++)
                {
                    dice.Add(Random.Shared.Next(1, 7));
                }

                dice.Sort();
                //Sum the highest 3 dice
                rolledPool[j] = dice[1] + dice[2] + dice[3];
            }

            rolledPools.Add(rolledPool);
        }

        rolledPools.AddRange(StatPools);

        int[] pickedPool = Random.Shared.GetItems(rolledPools.ToArray(), 1).First();
        Random.Shared.Shuffle(pickedPool);

        return new Statistics(
            pickedPool[0],
            pickedPool[1],
            pickedPool[2],
            pickedPool[3],
            pickedPool[4],
            pickedPool[5]
        );
    }

    static DerivedStatistics GenerateDerivedStats(Statistics stats)
    {
        int hp = (int)Math.Ceiling((stats.Strength + stats.Constitution) / 2.0);
        int wp = stats.Power;
        int san = stats.Power * 5;
        int breakingPoint = san - stats.Power;

        return new DerivedStatistics(hp, wp, san, breakingPoint);
    }

    static Dictionary<string, int> GenerateSkills(
        Profession profession,
        Nation ownNation,
        bool verbose = false
    )
    {
        Dictionary<string, int> skills = [];

        foreach (KeyValuePair<string, int> skill in DefaultSkills)
        {
            string skillName = GetSkillTypes(skill.Key, ownNation);
            skills[skillName] = skill.Value;
        }

        if (verbose)
            Console.WriteLine($"Default Skills: {string.Join(", ", skills)}");

        foreach (KeyValuePair<string, int> skill in profession.Skills.Always)
        {
            string skillName = GetSkillTypes(skill.Key, ownNation);
            if (skills.TryGetValue(skill.Key, out int value))
            {
                skills[skillName] = Math.Max(value, skill.Value);
            }
            else
            {
                skills[skillName] = skill.Value;
            }
        }

        if (verbose)
            Console.WriteLine(
                $"Profession Always Skills: {string.Join(", ", profession.Skills.Always)}"
            );

        List<string> pickedSkills = [];
        if (profession.Skills.PickAmount > 0)
        {
            string[] possibleSkills = [.. profession.Skills.Pick.Keys];
            Random.Shared.Shuffle(possibleSkills);

            for (int i = 0; i < profession.Skills.PickAmount; i++)
            {
                if (i >= possibleSkills.Length)
                    break;

                string skillName = possibleSkills[i];
                string newSkillName = GetSkillTypes(skillName, ownNation);
                if (skills.TryGetValue(skillName, out int value))
                {
                    skills[newSkillName] = Math.Max(value, profession.Skills.Pick[skillName]);
                }
                else
                {
                    skills[newSkillName] = profession.Skills.Pick[skillName];
                }
                pickedSkills.Add(newSkillName);
            }
        }

        if (verbose)
            Console.WriteLine($"Profession Pick Skills: {string.Join(", ", pickedSkills)}");

        // add bonus skills
        // Load skill packs from YAML file
        string yamlContent = File.ReadAllText("data/bonus_skills.yaml");
        IDeserializer deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        Dictionary<string, List<string>> bonusSkills = deserializer.Deserialize<
            Dictionary<string, List<string>>
        >(yamlContent);

        // Pick a random skill pack
        string bonusSkillPackName = Random.Shared.GetItems(bonusSkills.Keys.ToArray(), 1).First();
        List<string> bonusSkillsList = bonusSkills[bonusSkillPackName];

        if (verbose)
            Console.WriteLine($"Bonus Skill Pack: {bonusSkillPackName}");

        int boost = 20;
        int maxSkillLevel = 80;
        List<string> pickedBonusSkills = [];
        for (int i = 0; i < 8; i++)
        {
            string bonusSkill;
            if (i < bonusSkillsList.Count)
            {
                bonusSkill = bonusSkillsList[i];
            }
            else
            {
                //Get a random skill from the list of all bonus skills
                bonusSkill = Random.Shared.GetItems(AllBonusSkills.ToArray(), 1).First();
            }

            if (bonusSkill.Contains('|'))
            {
                string[] skillSelections = bonusSkill.Split('|');
                bonusSkill = Random.Shared.GetItems(skillSelections, 1).First();
            }

            bonusSkill = GetSkillTypes(bonusSkill, ownNation);

            if (
                skills.TryGetValue(bonusSkill, out int currentValue)
                && currentValue >= maxSkillLevel
            )
            {
                continue;
            }

            skills[bonusSkill] = Math.Min(currentValue + boost, 80);
            pickedBonusSkills.Add(bonusSkill);
        }

        if (verbose)
            Console.WriteLine($"Bonus Skill Pack: {string.Join(", ", pickedBonusSkills)}");

        return skills;
    }

    static string GetSkillTypes(string skillName, Nation ownNation)
    {
        if (skillName.Contains("(*)"))
        {
            string yamlContent = File.ReadAllText("data/skill_types.yaml");
            IDeserializer deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            Dictionary<string, List<string>> skillTypes = deserializer.Deserialize<
                Dictionary<string, List<string>>
            >(yamlContent);

            skillName = skillName[..^4]; // Remove the " (*)" part

            if (skillName == "foreign language")
            {
                yamlContent = File.ReadAllText("data/nations.yaml");
                Dictionary<string, Nation> nations = deserializer.Deserialize<
                    Dictionary<string, Nation>
                >(yamlContent);

                Nation[] otherNations = nations
                    .Values.Where(n => n.NativeLanguage != ownNation.NativeLanguage)
                    .ToArray();
                Nation randomNation = Random.Shared.GetItems(otherNations, 1).First();

                return $"{skillName} ({randomNation.NativeLanguage})";
            }

            if (skillTypes.TryGetValue(skillName, out List<string>? skillTypeSelections))
            {
                string skillType = Random.Shared.GetItems(skillTypeSelections.ToArray(), 1).First();
                return $"{skillName} ({skillType})";
            }
            else
            {
                throw new KeyNotFoundException(
                    $"Skill type for '{skillName}' not found in skill_types.yaml"
                );
            }
        }
        else
        {
            return skillName;
        }
    }
}
