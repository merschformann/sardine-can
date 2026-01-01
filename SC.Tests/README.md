# SC.Tests

Currently, this project only contains basic golden file tests for basic
end-to-end functionality verification.

## Update Golden Files

Normally, the golden files can be updated by running the tests with the `UPDATE`
environment variable set to `true`. For example on Windows PowerShell:

```ps
$env:UPDATE="true"
dotnet test
```

However, on Linux there have been issues with the used frameworks, a quick and
dirty way to update the golden files is to use the playground project
(potentially update the func used first):

```bash
# Change to project root
dotnet run --project SC.Playground/SC.Playground.csproj
# Select option 9
```
