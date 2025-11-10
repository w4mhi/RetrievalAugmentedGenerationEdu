# playground
Private playground

## Structure

This repository contains .NET 9.0 libraries with centralized package management.

### Projects

- **Playground.Core** - Core library project
- **Playground.Common** - Common utilities library project

### Centralized Package Management

This repository uses [Central Package Management (CPM)](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management) to manage NuGet package versions centrally.

#### Key Files

- **Directory.Packages.props** - Defines all package versions centrally
- **Directory.Build.props** - Defines common MSBuild properties for all projects
- **Playground.sln** - Solution file containing all library projects

#### How It Works

1. Package versions are defined in `Directory.Packages.props` at the repository root
2. Individual projects reference packages without specifying versions
3. All projects automatically use the versions defined in `Directory.Packages.props`

#### Adding a New Package

To add a new package to a project:

1. Add the package version to `Directory.Packages.props`:
   ```xml
   <PackageVersion Include="PackageName" Version="x.y.z" />
   ```

2. Reference the package in your project file without a version:
   ```xml
   <PackageReference Include="PackageName" />
   ```

#### Building

```bash
dotnet restore
dotnet build
```

