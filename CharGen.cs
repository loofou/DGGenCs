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

    static readonly Dictionary<string, int> DefaultSkills = new()
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
        { "unnatural", 0 },
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
        CharacterType type,
        Profession profession,
        int minAge,
        int maxAge,
        Sex? sexOverride = null,
        string? labelOverride = null,
        string? employerOverride = null,
        bool randomNationality = false,
        bool veteran = false,
        bool damaged = false,
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

        Statistics stats = GenerateStats(type, profession, demographics.Age);
        if (verbose)
            Console.WriteLine(stats);
        DerivedStatistics derivedStats = GenerateDerivedStats(stats);
        if (verbose)
            Console.WriteLine(derivedStats);
        Dictionary<string, int> skills = GenerateSkills(
            type,
            profession,
            demographics.Nationality,
            demographics.Age,
            verbose
        );
        List<string> bonds = GenerateBonds(profession, verbose);
        List<string> specialTraining = GenerateSpecialTraining(profession, verbose);

        List<Weapon> attacks = GenerateAttacks(profession, skills, verbose);
        List<Armor> armor = GenerateArmor(profession, verbose);
        List<string> equipment = GenerateEquipment(profession, verbose);

        List<string> motivations = GenerateMotivations(verbose);

        //veterancy
        if (veteran)
        {
            GenerateVeterancy(ref skills, verbose: verbose);
        }

        //damage
        if (damaged)
        {
            GenerateDamage(
                ref stats,
                ref derivedStats,
                ref skills,
                ref bonds,
                out List<string> disorders,
                verbose
            );
            motivations.AddRange(disorders);
        }

        return new(
            name,
            profession,
            demographics,
            stats,
            derivedStats,
            skills,
            bonds,
            specialTraining,
            attacks,
            armor,
            equipment,
            motivations
        );
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

    static Statistics GenerateStats(CharacterType type, Profession profession, int age)
    {
        if (type == CharacterType.Agent)
        {
            //Create random rolled pool of stats
            List<int[]> rolledPools = [];
            for (int i = 0; i < 3; i++)
            {
                int[] rolledPool = new int[6];
                for (int j = 0; j < rolledPool.Length; j++)
                {
                    //Roll 4 d6 and drop the lowest die
                    List<int> dice = [];
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
        else
        {
            NPCType npcType = AgeToNPCType(age);
            int[] stats = new int[6];
            List<int> importantStats = StatsToNr(profession.Npc.ImportantStats);

            switch (npcType)
            {
                //Child: Most 5, Important 7
                case NPCType.Child:
                {
                    for (int i = 0; i < stats.Length; i++)
                    {
                        if (importantStats.Contains(i))
                            stats[i] = 7;
                        else
                            stats[i] = 5;
                    }
                    break;
                }
                //Youth: Most 7, Important 9
                case NPCType.Youth:
                    for (int i = 0; i < stats.Length; i++)
                    {
                        if (importantStats.Contains(i))
                            stats[i] = 9;
                        else
                            stats[i] = 7;
                    }
                    break;
                //Novice + Ordinary: Most 10, Important 12
                case NPCType.Novice:
                case NPCType.Ordinary:
                    for (int i = 0; i < stats.Length; i++)
                    {
                        if (importantStats.Contains(i))
                            stats[i] = 12;
                        else
                            stats[i] = 10;
                    }
                    break;
                //Expert: Most 12, Important 14
                case NPCType.Expert:
                    for (int i = 0; i < stats.Length; i++)
                    {
                        if (importantStats.Contains(i))
                            stats[i] = 14;
                        else
                            stats[i] = 12;
                    }
                    break;
            }

            return new Statistics(stats[0], stats[1], stats[2], stats[3], stats[4], stats[5]);
        }
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
        CharacterType type,
        Profession profession,
        Nation ownNation,
        int age,
        bool verbose = false
    )
    {
        Dictionary<string, int> skills = [];
        if (type == CharacterType.Agent)
        {
            int maxSkillLevel = 80;
            foreach (KeyValuePair<string, int> skill in DefaultSkills)
            {
                string skillName = GetSkillTypes(skill.Key, ownNation);
                skills[skillName] = Math.Min(maxSkillLevel, skill.Value);
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
            string bonusSkillPackName = Random
                .Shared.GetItems(bonusSkills.Keys.ToArray(), 1)
                .First();
            List<string> bonusSkillsList = bonusSkills[bonusSkillPackName];

            if (verbose)
                Console.WriteLine($"Bonus Skill Pack: {bonusSkillPackName}");

            int boost = 20;
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
        }
        else
        {
            //NPCs
            NPCType npcType = AgeToNPCType(age);
            List<string> importantSkills = profession.Npc.ImportantSkills;
            int maxNormalSkillLevel = 100;
            int importantSkillLevel = 60;

            switch (npcType)
            {
                case NPCType.Child:
                    maxNormalSkillLevel = 10;
                    importantSkillLevel = 20;
                    break;
                case NPCType.Youth:
                    maxNormalSkillLevel = 30;
                    importantSkillLevel = 30;
                    break;
                case NPCType.Novice:
                    importantSkillLevel = 40;
                    break;
                case NPCType.Ordinary:
                    importantSkillLevel = 50;
                    break;
            }

            foreach (KeyValuePair<string, int> skill in DefaultSkills)
            {
                string skillName = GetSkillTypes(skill.Key, ownNation);
                skills[skillName] = Math.Min(maxNormalSkillLevel, skill.Value);
            }

            foreach (string importantSkill in importantSkills)
            {
                string skillName = GetSkillTypes(importantSkill, ownNation);
                if (skills.TryGetValue(skillName, out int value))
                {
                    skills[skillName] = Math.Max(value, importantSkillLevel);
                }
                else
                {
                    skills[skillName] = importantSkillLevel;
                }
            }

            if (verbose)
                Console.WriteLine($"NPC Skills: {string.Join(", ", skills)}");
        }

        return skills;
    }

    static string GetSkillTypes(string skillName, Nation ownNation)
    {
        if (skillName.Contains("(*"))
        {
            string yamlContent = File.ReadAllText("data/skill_types.yaml");
            IDeserializer deserializer = new DeserializerBuilder()
                .IgnoreUnmatchedProperties()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            Dictionary<string, List<string>> skillTypes = deserializer.Deserialize<
                Dictionary<string, List<string>>
            >(yamlContent);

            // Remove everything after "skillName" and before "(*...)" pattern
            int idx = skillName.IndexOf("(*");
            if (idx >= 0)
            {
                skillName = skillName[..idx].Trim();
            }

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

    static List<string> GenerateBonds(Profession profession, bool verbose = false)
    {
        string file = "data/bonds.txt";
        string[] bondTemplates = File.ReadAllLines(file);

        List<string> bonds = [];
        if (profession.Bonds is not 0)
        {
            bonds.AddRange(Random.Shared.GetItems(bondTemplates, profession.Bonds));
        }

        if (verbose)
            Console.WriteLine($"Bonds: {string.Join(", ", bonds)}");

        return bonds;
    }

    static List<string> GenerateSpecialTraining(Profession profession, bool verbose = false)
    {
        string yamlContent = File.ReadAllText("data/special_training.yaml");
        IDeserializer deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        Dictionary<string, Dictionary<string, string>> specialTrainingList =
            deserializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(yamlContent);

        List<string> specialTrainings = [];
        if (profession.SpecialTraining.chance > 0)
        {
            foreach (string training in profession.SpecialTraining.trainings)
            {
                if (Random.Shared.Next(0, 100) < profession.SpecialTraining.chance)
                {
                    if (
                        specialTrainingList.TryGetValue(
                            training,
                            out Dictionary<string, string>? trainingDetails
                        )
                    )
                    {
                        string formattedTraining =
                            $"{trainingDetails["name"]} ({trainingDetails["link"]})";
                        specialTrainings.Add(formattedTraining);
                    }
                    else
                    {
                        throw new KeyNotFoundException(
                            $"Special training '{training}' not found in special_training.yaml"
                        );
                    }
                }
            }
        }

        if (verbose)
            Console.WriteLine($"Special Training: {string.Join(", ", specialTrainings)}");

        return specialTrainings;
    }

    static List<Weapon> GenerateAttacks(
        Profession profession,
        Dictionary<string, int> skills,
        bool verbose = false
    )
    {
        string yamlContent = File.ReadAllText("data/gear.yaml");
        IDeserializer deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        Gear gear = deserializer.Deserialize<Gear>(yamlContent);

        yamlContent = File.ReadAllText("data/gear_kits.yaml");
        Dictionary<string, GearKit> gearKits = deserializer.Deserialize<
            Dictionary<string, GearKit>
        >(yamlContent);

        string gearKitName = profession.GearKit;

        List<Weapon> attacks = [];
        if (gearKitName is not null && gearKits.TryGetValue(gearKitName, out GearKit gearKit))
        {
            if (gearKit.Weapons is not null)
            {
                foreach (GearItem weaponItem in gearKit.Weapons)
                {
                    if (
                        weaponItem.Chance is not null
                        && Random.Shared.Next(0, 100) >= weaponItem.Chance
                    )
                    {
                        continue; // Skip this weapon if the chance condition is not met
                    }

                    if (gear.Weapons.TryGetValue(weaponItem.Item, out Weapon weapon))
                    {
                        if (skills.TryGetValue(weapon.Skill, out int skillValue))
                        {
                            attacks.Add(weapon);
                        }
                        else
                        {
                            throw new KeyNotFoundException(
                                $"Weapon Skill '{weapon.Skill}' not found in learned skills. ({weaponItem.Item})"
                            );
                        }
                    }
                    else
                    {
                        throw new KeyNotFoundException(
                            $"Weapon '{weaponItem.Item}' not found in gear.yaml"
                        );
                    }
                }
            }
        }
        else
        {
            throw new KeyNotFoundException($"Gear kit '{gearKitName}' not found in gear_kits.yaml");
        }

        if (verbose)
            Console.WriteLine($"Attacks: {string.Join(", ", attacks)}");

        return attacks;
    }

    static List<Armor> GenerateArmor(Profession profession, bool verbose = false)
    {
        string yamlContent = File.ReadAllText("data/gear.yaml");
        IDeserializer deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        Gear gear = deserializer.Deserialize<Gear>(yamlContent);

        yamlContent = File.ReadAllText("data/gear_kits.yaml");
        Dictionary<string, GearKit> gearKits = deserializer.Deserialize<
            Dictionary<string, GearKit>
        >(yamlContent);

        string gearKitName = profession.GearKit;

        List<Armor> armor = [];
        if (gearKitName is not null && gearKits.TryGetValue(gearKitName, out GearKit gearKit))
        {
            if (gearKit.Armor is not null)
            {
                foreach (GearItem armorItem in gearKit.Armor)
                {
                    if (
                        armorItem.Chance is not null
                        && Random.Shared.Next(0, 100) >= armorItem.Chance
                    )
                    {
                        continue; // Skip this armor if the chance condition is not met
                    }

                    if (gear.Armor.TryGetValue(armorItem.Item, out Armor armorPiece))
                    {
                        armor.Add(armorPiece);
                    }
                    else
                    {
                        throw new KeyNotFoundException(
                            $"Armor '{armorItem.Item}' not found in gear.yaml"
                        );
                    }
                }
            }
        }
        else
        {
            throw new KeyNotFoundException($"Gear kit '{gearKitName}' not found in gear_kits.yaml");
        }

        if (verbose)
            Console.WriteLine($"Armor: {string.Join(", ", armor)}");

        return armor;
    }

    static List<string> GenerateEquipment(Profession profession, bool verbose = false)
    {
        string yamlContent = File.ReadAllText("data/gear.yaml");
        IDeserializer deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        Gear gear = deserializer.Deserialize<Gear>(yamlContent);

        yamlContent = File.ReadAllText("data/gear_kits.yaml");
        Dictionary<string, GearKit> gearKits = deserializer.Deserialize<
            Dictionary<string, GearKit>
        >(yamlContent);

        string gearKitName = profession.GearKit;

        List<string> equipment = [];
        if (gearKitName is not null && gearKits.TryGetValue(gearKitName, out GearKit gearKit))
        {
            if (gearKit.Other is not null)
            {
                foreach (GearItem otherItem in gearKit.Other)
                {
                    if (
                        otherItem.Chance is not null
                        && Random.Shared.Next(0, 100) >= otherItem.Chance
                    )
                    {
                        continue; // Skip this item if the chance condition is not met
                    }

                    if (gear.Other.TryGetValue(otherItem.Item, out string? item))
                    {
                        equipment.Add(item);
                    }
                    else
                    {
                        throw new KeyNotFoundException(
                            $"Other item '{otherItem.Item}' not found in gear.yaml"
                        );
                    }
                }
            }
        }
        else
        {
            throw new KeyNotFoundException($"Gear kit '{gearKitName}' not found in gear_kits.yaml");
        }

        if (verbose)
            Console.WriteLine($"Equipment: {string.Join(", ", equipment)}");

        return equipment;
    }

    static List<string> GenerateMotivations(bool verbose = false)
    {
        string yamlContent = File.ReadAllText("data/motivations.yaml");
        IDeserializer deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        Dictionary<string, Motivation> motivationMap = deserializer.Deserialize<
            Dictionary<string, Motivation>
        >(yamlContent);

        List<Motivation> motivationRandomList = [];
        foreach (var pair in motivationMap)
        {
            for (int i = 0; i < pair.Value.chances; i++)
            {
                motivationRandomList.Add(pair.Value);
            }
        }

        List<string> motivations = [];
        int motivationAmount = Random.Shared.Next(1, 4); // 1 - 3
        for (int i = 0; i < motivationAmount; i++)
        {
            Motivation motivationObj = Random
                .Shared.GetItems(motivationRandomList.ToArray(), 1)
                .First();

            string motivation = Random
                .Shared.GetItems(motivationObj.motivation.ToArray(), 1)
                .First();

            if (motivationObj.objects is not null)
            {
                motivation +=
                    " " + Random.Shared.GetItems(motivationObj.objects.ToArray(), 1).First();
            }

            motivations.Add(motivation);
        }

        if (verbose)
            Console.WriteLine($"Motivations: {string.Join(", ", motivations)}");

        return motivations;
    }

    static void GenerateVeterancy(
        ref Dictionary<string, int> skills,
        int numberOfSkills = 10,
        int min = 1,
        int max = 4,
        int maxSkill = 100,
        bool noOccult = false,
        bool verbose = false
    )
    {
        List<string> skillsChanged = [];

        int currentSkillImprovements = 0;
        string[] skillNames = [.. skills.Keys];
        Random.Shared.Shuffle(skillNames);

        foreach (string skill in skillNames)
        {
            if (skill == "unnatural" || (skill == "occult" && noOccult))
                continue;

            if (Random.Shared.Next(1, 100) > skills[skill])
            {
                if (min < max)
                {
                    skills[skill] = Math.Min(
                        skills[skill] + Random.Shared.Next(min, max),
                        maxSkill
                    );
                }
                else
                {
                    skills[skill] = Math.Min(skills[skill] + min, maxSkill);
                }
                skillsChanged.Add(skill);
                currentSkillImprovements++;
            }

            if (currentSkillImprovements >= numberOfSkills)
                break;
        }

        if (verbose)
            Console.WriteLine($"Veterancy: skills changed {string.Join(", ", skillsChanged)}");
    }

    static void GenerateDamage(
        ref Statistics stats,
        ref DerivedStatistics derivedStatistics,
        ref Dictionary<string, int> skills,
        ref List<string> bonds,
        out List<string> disorders,
        bool verbose = false
    )
    {
        disorders = [];
        int type = Random.Shared.Next(0, 100);

        if (type < 30)
        {
            //Extreme Violence
            skills["occult"] += 10;
            derivedStatistics.SAN -= 5;
            stats.Charisma -= 3;
            disorders.Add("Adapted to violence");

            if (verbose)
                Console.WriteLine("Damaged: Extreme Violence");
        }
        else if (type < 60)
        {
            // Captivity
            skills["occult"] += 10;
            derivedStatistics.SAN -= 5;
            stats.Charisma -= 3;
            disorders.Add("Adapted to helplessness");

            if (verbose)
                Console.WriteLine("Damaged: Captivity");
        }
        else if (type < 90)
        {
            //Hard Experience
            skills["occult"] += 10;
            GenerateVeterancy(ref skills, 5, 10, 10, 90, true, verbose);
            bonds.Remove(Random.Shared.GetItems(bonds.ToArray(), 1).First());

            if (verbose)
                Console.WriteLine("Damaged: Hard Experience");
        }
        else
        {
            //Things Man Was Not Meant to Know
            string file = "data/disorders.txt";
            string[] disorderList = File.ReadAllLines(file);

            skills["unnatural"] += 10;
            skills["occult"] += 20;
            derivedStatistics.SAN -= stats.Power;
            derivedStatistics.BreakingPoint = derivedStatistics.SAN - stats.Power;
            disorders.Add(Random.Shared.GetItems(disorderList, 1).First());

            if (verbose)
                Console.WriteLine("Damaged: Things Man Was Not Meant to Know");
        }
    }

    static NPCType AgeToNPCType(int age)
    {
        if (age < 14)
        {
            return NPCType.Child;
        }
        else if (age < 21)
        {
            return NPCType.Youth;
        }
        else if (age < 30)
        {
            return NPCType.Novice;
        }
        else if (age < 40)
        {
            return NPCType.Ordinary;
        }
        else
        {
            return NPCType.Expert;
        }
    }

    static int StatToNr(string statName)
    {
        return statName switch
        {
            "str" => 0,
            "dex" => 1,
            "con" => 2,
            "int" => 3,
            "pow" => 4,
            "cha" => 5,
            _ => throw new ArgumentException($"Unknown stat name: {statName}"),
        };
    }

    static List<int> StatsToNr(List<string> statNames)
    {
        List<int> statIndices = [];
        foreach (string statName in statNames)
        {
            statIndices.Add(StatToNr(statName));
        }
        return statIndices;
    }
}
