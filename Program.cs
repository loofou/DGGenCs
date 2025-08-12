using System.CommandLine;
using Spectre.Console;
using Spectre.Console.Cli.Help;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class Program
{
    public static int Main(string[] args)
    {
        AnsiConsole.MarkupLine("[teal bold]Delta Green Character Generator[/]");

        RootCommand rootCommand = new("Delta Green Character Generator");

        Option<string> professionOption = new("--profession", "-p")
        {
            Description = "Profession of the character to generate",
            DefaultValueFactory = _ => "cid",
            Arity = ArgumentArity.ZeroOrOne,
        };
        rootCommand.Options.Add(professionOption);

        Option<int> countOption = new("--count", "-c")
        {
            Description = "Number of characters to generate",
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

        Option<bool> verboseOption = new("--verbose", "-v")
        {
            Description = "Enable verbose output",
            DefaultValueFactory = _ => false,
            Arity = ArgumentArity.ZeroOrOne,
        };
        rootCommand.Options.Add(verboseOption);

        rootCommand.SetAction(
            (parseResult) =>
            {
                int count = parseResult.GetValue(countOption);
                string professionName = parseResult.GetValue(professionOption) ?? "cid"; //TODO: Randomize
                bool randomNationality = parseResult.GetValue(randomNationalityOption);

                bool verbose = parseResult.GetValue(verboseOption);

                Generate(count, professionName, randomNationality, verbose);
            }
        );

        if (!Directory.Exists("out"))
            Directory.CreateDirectory("out");

        ParseResult parseResult = rootCommand.Parse(args);
        return parseResult.Invoke();
    }

    private static void Generate(
        int count,
        string professionName,
        bool randomNationality,
        bool verbose
    )
    {
        for (int i = 0; i < count; i++)
        {
            Profession profession = GetProfession(professionName);

            AnsiConsole.MarkupLine($"[blue]Generating character {i + 1} of {count}...[/]");
            Character character = CharGen.GenerateNewCharacter(
                profession,
                randomNationality: randomNationality,
                verbose: verbose
            );

            AnsiConsole.MarkupLine($"[green]Character {character.Name} generated.[/]");
            string output = character.ToString();

            File.WriteAllText(Path.Combine("out", $"Character_{i + 1}.txt"), output);
        }
    }

    private static Profession GetProfession(string professionName)
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

        Profession profession = professions[professionName];
        return profession;
    }
}
