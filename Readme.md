# DGGenCs

A Delta Green character statblock generator written in C# for .NET 9. It is losely based off of https://github.com/jimstorch/DGGen but with a few different design principles.

For once, this generator does not create PDFs. Instead it creates statblocks that can be easily shared as text or imported into VTTs, like FoundryVTT.

## Prerequisites
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) must be installed.

## Building the Project

1. Open a terminal in the project root directory.
2. Run the following command:

    dotnet build

This will build the project and output the binaries to the `bin/Debug/net9.0` directory by default.

## Running the Tool

After building, you can run the tool with:

```
./bin/Debug/net9.0/DGGenCs.exe
```


### Arguments

| Argument                | Short | Description                                                      | Default |
|-------------------------|-------|------------------------------------------------------------------|---------|
| --profession <name>     | -p    | Profession of the character to generate                          | cid     |
| --count <number>        | -c    | Number of characters to generate                                 | 1       |
| --random-nationality    | -r    | Generate characters with random nationalities (default: USA)      | false   |
| --verbose               | -v    | Enable verbose output                                            | false   |

### Examples

Generate a single FBI CID agent (default):

```
DGGenCs.exe
```

Generate 5 random agents of the "author" profession:

```
DGGenCs.exe --profession author --count 5
```

Generate 3 agents with random nationalities and verbose output:

```
DGGenCs.exe -c 3 -r -v
```

Generate 2 agents and output using the built executable:

```
DGGenCs.exe --count 2
```
