# <img src="assets/zbox.png" width="64"/>ZExtract

Steam .z file extraction utility.  Useful for those who download workshop files for servers using the steamcmd CLI.

## Requirements

* [dotnet core  runtime](https://dotnet.microsoft.com/download/dotnet-core/current/runtime)

## Download

See [Releases](https://github.com/davidruhmann/ZExtract/releases) for the CLI.

Or [Packages](https://github.com/davidruhmann/ZExtract/packages) for the NuGet package. (Also available at [NuGet.org](https://www.nuget.org/packages/ZExtract/))

```bash
dotnet add package ZExtract
```

Or build your own from source.

## Usage

```bash
dotnet ZExtractCLI myFile.asset.z outputDirectory\
```

Will extract the .z file to `outputDirectory\myFile.asset`

## Build

### Requirements

* [dotnet core sdk](https://dotnet.microsoft.com/download)

Checkout and run...

```bash
dotnet publish -c Release -o publish
```

then execute `publish/ZExtractCLI`
