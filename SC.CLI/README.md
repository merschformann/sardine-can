# SardineCan CLI

This project provides a simple Command-line interface for SardineCan. It allows
JSON-in/JSON-out usage.

## Example

Pipe an instance directly to stdin and get the result on stdout:

```bash
cat ../Material/REST/calculation.json | dotnet run
```

Let the SC.CLI read an instance from a file and write the result to a file:

```bash
dotnet run --input ../Material/REST/calculation.json --output output.json
```
