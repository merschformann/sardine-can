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

## Input format

Find a short outline of the basic input format entities below. Keys in
parentheses are optional.

```txt
[root object]
├── instance
|   ├── containers (array of)
|   │   ├── id: string // identifies the container
|   │   ├── length: float // side-length (x)
|   │   ├── width: float // side-length (y)
|   │   └── height: float // side-length (z)
|   ├── pieces (array of)
|   |   ├── id: string // identifies the piece
|   |   ├── cubes (array of)
|   |   |   ├── (x): float // x-offset from parent origin
|   |   |   ├── (y): float // y-offset from parent origin
|   |   |   ├── (z): float // z-offset from parent origin
|   |   |   ├── length: float // side-length (x)
|   |   |   ├── width: float // side-length (y)
|   |   |   └── height: float // side-length (z)
|   |   └── (flags) (array of)
|   |       ├── (flagId): int // flag rule this entry refers to (see rules)
|   |       └── (flagValue): float // value of this flag rule entry
|   └── (rules)
|       └── flagRules (array of)
|           ├── flagId: int // ID of the flag rule
|           ├── ruleType: string/enum // one of DISJOINT, LESSEREQUALSPIECES, GREATEREQUALSPIECES
|           └── parameter: int // min/max per container if <= or >= rule
├── (priority): int // job priority, lower priorities are executed first
└── (configuration)
    ├── (TimeLimitInSeconds): float // calculation is stopped after this duration
    └── (ThreadLimit): int // number of threads calculation may use (<=0 is all) 
```

## Output format

Find a short outline of the output format entities below.

```txt
[root object]
├── containers (array of)
|   └── assignments (array of)
|       ├── piece: int // ID of the piece
|       └── position: float // side-length (x)
|           ├── x: float // x-position within container
|           ├── y: float // y-position within container
|           ├── z: float // z-position within container
|           ├── a: float // degree rotation around x-axis
|           ├── b: float // degree rotation around y-axis
|           └── c: float // degree rotation around z-axis
└── offload (array of) // IDs of unassigned pieces
```

## Post a job

To post a job to the service, simply send a POST request to the `/packing/calculations` endpoint

```bash
curl -X 'POST' \
  'http://localhost:4550/packing/calculations' \
  -H 'accept: text/plain' \
  -H 'Content-Type: application/json' \
  -d '{
    "instance": {
        "name": "string",
        "containers": [
            {
                "id": 0,
                "length": 3,
                "width": 3,
                "height": 3
            },
            {
                "id": 1,
                "length": 4,
                "width": 4,
                "height": 4
            }
        ],
        "pieces": [
            {
                "id": 0,
                "flags": [
                    {
                        "flagId": 0,
                        "flagValue": 0
                    }
                ],
                "cubes": [
                    {
                        "length": 2,
                        "width": 2,
                        "height": 2
                    }
                ]
            },
            {
                "id": 1,
                "flags": [
                    {
                        "flagId": 0,
                        "flagValue": 0
                    }
                ],
                "cubes": [
                    {
                        "length": 2,
                        "width": 1,
                        "height": 1
                    }
                ]
            }
        ]
    }
}'
```

Poll the status until the job is finished (look for `.status == DONE`). The status is available via the `/packing/calculations/{id}/status` endpoint.

```bash
curl -X 'GET' \
  'http://localhost:4550/packing/calculations/0/status' \
  -H 'accept: text/plain'
```

Finally, retrieve the result via the `/packing/calculations/{id}/result` endpoint.

```bash
curl -X 'GET' \
  'http://localhost:4550/packing/calculations/0/solution' \
  -H 'accept: text/plain'
```
