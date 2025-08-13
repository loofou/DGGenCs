using System.CommandLine;
using Spectre.Console;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class Program
{
    public static int Main(string[] args)
    {
        AnsiConsole.MarkupLine("[teal bold]Delta Green Character Generator[/]");

        RootCommand rootCommand = new("Delta Green Character Generator");

        Option<CharacterType> typeOption = new("--type", "-t")
        {
            Description =
                "Type of character to generate (agent or npc). NPCs use the simpler stat creation method from the Handlers Guide p. 354. Agents on the other hand use the full stat creation system from the Agents Handbook. Age is taken into account only for NPCs!",
            DefaultValueFactory = _ => CharacterType.Agent,
            Arity = ArgumentArity.ZeroOrOne,
        };
        rootCommand.Options.Add(typeOption);

        Option<string> professionOption = new("--profession", "-p")
        {
            Description = "Profession of the character to generate. Otherwise picked randomly.",
            Arity = ArgumentArity.ZeroOrOne,
        };
        rootCommand.Options.Add(professionOption);

        Option<int> countOption = new("--count", "-c")
        {
            Description = "Number of characters to generate.",
            DefaultValueFactory = _ => 1,
            Arity = ArgumentArity.ZeroOrOne,
        };
        countOption.Validators.Add(result =>
        {
            if (result.GetValue(countOption) < 1)
            {
                result.AddError("The count must be at least 1.");
            }
        });
        rootCommand.Options.Add(countOption);

        Option<bool> randomNationalityOption = new("--random-nationality", "-r")
        {
            Description = "Generate characters with random nationalities. Otherwise uses the USA.",
            DefaultValueFactory = _ => false,
            Arity = ArgumentArity.ZeroOrOne,
        };
        rootCommand.Options.Add(randomNationalityOption);

        Option<int[]> ageOption = new("--age", "-a")
        {
            Description =
                "Age range for the character (min,max) or single number for constant age.",
            DefaultValueFactory = _ => [25, 55],
            Arity = ArgumentArity.ZeroOrMore,
            AllowMultipleArgumentsPerToken = true,
        };
        ageOption.Validators.Add(result =>
        {
            int[] ages = result.GetValue(ageOption) ?? [];
            if (ages.Length == 1)
            {
                // If only one age is provided, treat it as a constant age
                ages = [ages[0], ages[0]];
            }
            if (ages.Length != 2 || ages[0] < 0 || ages[1] < 0 || ages[0] > ages[1])
            {
                result.AddError(
                    "The age must be specified as one or two positive integers in the format min,max."
                );
            }
        });
        rootCommand.Options.Add(ageOption);

        Option<bool> veteranOption = new("--veteran", "-v")
        {
            Description = "Generate characters with a few random skill increases already.",
            DefaultValueFactory = _ => false,
            Arity = ArgumentArity.ZeroOrOne,
        };
        rootCommand.Options.Add(veteranOption);

        Option<bool> damagedOption = new("--damaged", "-d")
        {
            Description =
                "Generate characters with exposure to the Unnatural, leaving them damaged.",
            DefaultValueFactory = _ => false,
            Arity = ArgumentArity.ZeroOrOne,
        };
        rootCommand.Options.Add(damagedOption);

        Option<bool> verboseOption = new("--verbose")
        {
            Description = "Enable verbose output for debug purposes.",
            DefaultValueFactory = _ => false,
            Arity = ArgumentArity.ZeroOrOne,
        };
        rootCommand.Options.Add(verboseOption);

        rootCommand.SetAction(
            (parseResult) =>
            {
                int count = parseResult.GetValue(countOption);
                CharacterType type = parseResult.GetValue(typeOption);
                string? professionName = parseResult.GetValue(professionOption);
                bool randomNationality = parseResult.GetValue(randomNationalityOption);
                int[] ageRange = parseResult.GetValue(ageOption) ?? [25, 55];
                if (ageRange.Length == 1)
                {
                    ageRange = [ageRange[0], ageRange[0]];
                }

                bool veteran = parseResult.GetValue(veteranOption);
                bool damaged = parseResult.GetValue(damagedOption);

                bool verbose = parseResult.GetValue(verboseOption);

                Generate(
                    type,
                    count,
                    professionName,
                    ageRange,
                    randomNationality,
                    veteran,
                    damaged,
                    verbose
                );
            }
        );

        if (!Directory.Exists("out"))
            Directory.CreateDirectory("out");

        ParseResult parseResult = rootCommand.Parse(args);
        return parseResult.Invoke();
    }

    private static void Generate(
        CharacterType type,
        int count,
        string? professionName,
        int[] ageRange,
        bool randomNationality,
        bool veteran,
        bool damaged,
        bool verbose
    )
    {
        for (int i = 0; i < count; i++)
        {
            Profession profession = GetProfession(professionName);

            AnsiConsole.MarkupLine($"[blue]Generating character {i + 1} of {count}...[/]");

            if (verbose)
            {
                Console.WriteLine($"Profession: {profession}");
            }

            Character character = CharGen.GenerateNewCharacter(
                type,
                profession,
                ageRange[0],
                ageRange[1],
                veteran: veteran,
                damaged: damaged,
                randomNationality: randomNationality,
                verbose: verbose
            );

            AnsiConsole.MarkupLine($"[green]Character {character.Name} generated.[/]");
            string output = character.ToString();

            File.WriteAllText(Path.Combine("out", $"Character_{i + 1}.txt"), output);
        }
    }

    private static Profession GetProfession(string? professionName)
    {
        IDeserializer deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        // Load nations from YAML file
        string yamlContent = File.ReadAllText("data/professions.yaml");
        Dictionary<string, Profession> professions = deserializer.Deserialize<
            Dictionary<string, Profession>
        >(yamlContent);

        Profession profession;
        if (professionName is not null)
        {
            profession = professions[professionName];
        }
        else
        {
            profession = Random.Shared.GetItems(professions.Values.ToArray(), 1).First();
        }

        if (profession.Override is not null)
        {
            if (professions.TryGetValue(profession.Override, out Profession overriddenProfession))
            {
                profession = overriddenProfession with
                {
                    Employer = profession.Employer,
                    Division = profession.Division,
                };
            }
            else
            {
                throw new KeyNotFoundException(
                    $"Profession override '{profession.Override}' not found in professions.yaml"
                );
            }
        }

        return profession;
    }
}
