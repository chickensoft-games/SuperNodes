# SharedPowerUps

An example of how to share PowerUps as a source-only nuget package for use in Godot C# game projects.

This is a source-only nuget package. When imported into a project, the code in this package will be compiled with the code in the project that imports this package. If the project using this package has source generators, this code will be seen by those source generators.

Since this package doesn't have dependencies, it's natural for your IDE to show errors that the PowerUp attribute doesn't exist. To make development tolerable, we recommend developing PowerUps in an actual game project and verifying they work correctly *before* packaging them up as source-only packages.

## Local Usage Notes

Importing source-only packages locally via `<ProjectReference>` doesn't work, as the imported source is not fed to the consuming project's source generators.

To use a source-only PowerUp package locally, first build the project.

```sh
cd SharedPowerUps # or wherever your source-only PowerUp project is
dotnet build
```

Add a `nuget.config` alongside your solution file for the project in which you want to import your source-only PowerUp package.

In the `nuget.config` file, add a key (any name will work) with the value containing the path to your source-only PowerUp package:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
	<config>
	</config>
	<settings>
	</settings>
	<packageSources>
		<add key="Local Packages" value="/Somewhere/LocalPackages" />
	</packageSources>
</configuration>
```

The path in the `nuget.config` to the local package should be the same as the `<OutputPath>` in the source-only PowerUp package's `*.csproj` file.

Finally, in your game project, add a `<PackageReference>` to the source-only PowerUp package. The `nuget.config` file will instruct the `dotnet` tool to resolve your package from the local path.

You must include `PrivateAssets="all"`.

```xml
<ItemGroup>
  <PackageReference Include="SharedPowerUps" Version="1.0.0" PrivateAssets="all" />
</ItemGroup>
```

If you make changes to your source-only PowerUp package, `dotnet restore` will not always pick up on the changes. To force your game project to pull in the latest package, run the following:

```sh
cd YourGameProject
dotnet nuget locals --clear all
dotnet build
```
