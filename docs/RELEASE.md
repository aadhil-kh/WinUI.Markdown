# Release Process

1. Update `PackageVersion` in `Directory.Build.props`.
2. Update `CHANGELOG.md`.
3. Run:

   ```powershell
   dotnet restore MarkdownView.slnx
   dotnet build MarkdownView.slnx -c Release --no-restore
   dotnet test tests\WinUI.Markdown.Tests\WinUI.Markdown.Tests.csproj -c Release --no-build
   dotnet pack src\WinUI.Markdown\MarkdownView.csproj -c Release --no-build --no-restore -o artifacts
   ```

4. Inspect the generated `.nupkg` and `.snupkg`.
5. Create and push a version tag:

   ```powershell
   git tag v0.1.0
   git push origin v0.1.0
   ```

6. The publish workflow pushes packages to NuGet.org using `NUGET_API_KEY`.

Repository setup required before first publish:

- Create a NuGet.org API key with push rights.
- Add it as a GitHub repository secret named `NUGET_API_KEY`.
- Confirm `RepositoryUrl` and `PackageProjectUrl` in `Directory.Build.props`.
