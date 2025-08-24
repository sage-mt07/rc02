`dotnet restore` failed with NU1301 errors because NuGet was unreachable in the Codex environment.
`dotnet test` therefore failed during the restore step. The test project includes
`IsTestProject` and references `xunit`, `xunit.runner.visualstudio`, and `Moq`
so tests should run once packages can be restored.
Run these commands locally with internet access to execute the suite.
