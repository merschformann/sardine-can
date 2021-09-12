# SardineCan service

This project provides a simple job manager serving SardineCan via a RESTful
service.

## Docker usage

The SardineCan service is available via docker image. Spin it up via:

```bash
docker run -d --restart always -p 4550:80 --name sardinecan ghcr.io/merschformann/sardinecan:latest
```

## dotnet usage

To start the service directly simply run:

```bash
dotnet run --urls=http://localhost:4550/
```

## Swagger

After successful deployment of the service, a swagger UI is available here:

```txt
http://localhost:4550/swagger/index.html
```

## Configuration

The service itself may be configured either via the `appsettings.json` or via
corresponding environment variables. Latter take precedence over values set in
`appsettings.json` and can additionally be configured for the docker container
(e.g.: use `--env MAX_THREADS=2` to limit the thread count to 2).

All available configuration values are listed in the table below.

| Parameter            |  type |      json key |  env variable |
|:-------------------- | -----:| -------------:| -------------:|
| Maximal thread count | _int_ |  `MaxThreads` | `MAX_THREADS` |
