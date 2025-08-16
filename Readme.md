# DGGenCs

A Delta Green character statblock generator written in C# for .NET 9. It is losely based off of https://github.com/jimstorch/DGGen but with a few different design principles.

For once, this generator does not create PDFs. Instead it creates statblocks that can be easily shared as text or imported into VTTs, like FoundryVTT.

## Prerequisites
- [.NET 9](https://dotnet.microsoft.com/download/dotnet/9.0) must be installed to run the tool.
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) must be installed to build the tool yourself.

## Running the Generator

Download the latest release from github or build the tool yourself. Then you can run the tool via

```
.\DGGenCs.exe
```

(I give the windows powershell variant of commands, but the parameters are the same on all platforms)


### Arguments

| Argument             | Short | Description                                                                                                                                                                                                                         | Default |
| -------------------- | ----- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ------- |
| --type <agent\|npc>  | -t    | Type of character to generate (agent or npc). NPCs use the simpler stat creation method from the Handlers Guide p. 354. Agents use the full stat creation system from the Agents Handbook. Age is taken into account only for NPCs! | agent   |
| --profession <name>  | -p    | Profession of the character to generate. Otherwise picked randomly.                                                                                                                                                                 |         |
| --employer <name>    | -e    | Pick any profession of the given employer. Otherwise all employers are valid.                                                                                                                                                       |         |
| --count <number>     | -c    | Number of characters to generate                                                                                                                                                                                                    | 1       |
| --random-nationality | -r    | Generate characters with random nationalities (otherwise uses the USA)                                                                                                                                                              | false   |
| --age <min> [max]    | -a    | Age range for the character (min,max) or single number for constant age                                                                                                                                                             | 25 55   |
| --veteran            | -v    | Generate characters with a few random skill increases already                                                                                                                                                                       | false   |
| --damaged            | -d    | Generate characters with exposure to the Unnatural, leaving them damaged                                                                                                                                                            | false   |
| --verbose            |       | Enable verbose output for debug purposes.                                                                                                                                                                                           | false   |

### Examples

Generate a single agent with a random profession (default):

```
.\DGGenCs.exe
```

Generate a single agent with a profession from a specific employer:

```
.\DGGenCs.exe --employer ATF
```

Generate 5 random agents of the "federal_agent" profession:

```
.\DGGenCs.exe --profession federal_agent --count 5
```

Generate 3 agents with random nationalities and veterancy:

```
.\DGGenCs.exe -c 3 -r -v
```

Generate 2 damaged veteran agents:

```
.\DGGenCs.exe --count 2 -dv
```

## Building the Project

1. Open a terminal in the project root directory.
2. Run the following command:

``` 
dotnet build
```

This will build the project and output the binaries to the `bin/Debug/net9.0` directory by default.

After building, you can run the tool with:

```
./bin/Debug/net9.0/DGGenCs.exe
```

## License

DDGenCs's source code is licensed under the Unlicense (tldr: Do whatever you want with it, but don't expect support. Then again, feel free to ask for feature requests in the github Issues, I might just add them :) just no guarantee!).

Excepted from the license above is any mention or use of intellectual property known as Delta Green. The intellectual property known as Delta Green is ™ and © owned the Delta Green Partnership (http://www.delta-green.com). This tool is released under the "[FIELD REPORTS & FAN CREATIONS](https://www.delta-green.com/questions/)" guidelines.
