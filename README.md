# SardineCan

SardineCan is a humble 3D knapsack / bin packing solver with some special
constraints. It is a collection of constructive heuristics, meta-heuristic
attempts and linear models with CPLEX & Gurobi bindings.

In short, the code mainly derives from my (Marius Merschformann) master thesis
(2014) and was primarily uploaded to enable colleagues to use it for their
projects. However, I will be very happy, if it is useful to even more people. :)

![sample screenshot](Material/Screenshots/CO2.png "Sample screenshot")

## Quick intro

### Prerequisites

dotnet (core):

- _Windows_: download & install ([link](https://dotnet.microsoft.com/download))
- _Linux_: [instructions](https://docs.microsoft.com/en-us/dotnet/core/install/linux)

docker (for _SC.Service_ in docker container):

- Docker [instructions](https://docs.docker.com/get-docker/)

### Set SardineCan up as a service

SC comes with a very simple RESTful service. The steps below will set it up:

1. Clone this repository

    ```bash
    git clone https://github.com/merschformann/sardine-can.git
    ```

1. Navigate to service project dir

    ```bash
    cd sardine-can/SC.Service/
    ```

1. Build docker container

    ```bash
    ./docker-build.sh
    ```

1. Start docker container

    ```bash
    ./docker-run.sh
    ```

After deploying, a Swagger UI description of the RESTful service can be found
here: [http://localhost:4550/swagger](http://localhost:4550/swagger)
(above uses defaults, adjust them as desired)

### Build the GUI

--> Windows only :-/

1. Clone this repository

    ```bash
    git clone https://github.com/merschformann/sardine-can.git
    ```

1. Open _SardineCan.sln_ with Visual Studio (or other IDE)
1. Set _SC.GUI_ as startup project
1. Compile and execute

## Remarks

### Gurobi & CPLEX support

Unfortunately, I cannot ship the Gurobi and CPLEX libraries with the code.
I made an attempt of not relying on these during compile time by moving them to
a Nuget package (Atto.LinearWrap). Even though this part works, I had some
issues when supplying the dlls later on. Let me know, if you have ideas how to
overcome this.

I hope I can provide a solution for all who have access to Gurobi and/or CPLEX
in the future, so that the model formulations can also be tested.

### Contributors

The code mainly originated from the master-thesis of Marius Merschformann
(2014). Find a copy [here](./Material/MasterThesis/MasterThesis_MariusMerschformann.pdf).

The implementations around pre-processing were done by Daniel Erdmann and Simon
Moss during a university project. Further work on ALNS & some further extensions
were done in collaboration with Daniela Guericke.
