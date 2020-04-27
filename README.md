# SardineCan #

SardineCan is a humble 3D knapsack / bin packing solver with some special constraints. It is a collection of constructive heuristics, meta-heuristic attempts and linear models with CPLEX & Gurobi bindings.

In short, the code mainly derives from my (Marius Merschformann) master thesis (2014) and was primarily uploaded to enable colleagues to use it for their projects. However, I will be very happy, if it is useful to even more people. :)

![sample screenshot](Material/Screenshots/CO2.png "Sample screenshot")

### Quickly set it up as a service ###

SC comes with a very simple RESTful service. The steps below will quickly set it up:

1. git clone https://github.com/merschformann/sardine-can.git
1. cd sardine-can/SC.Service/
1. ./docker-build.sh
1. ./docker-run.sh

After deploying, a Swagger UI description of the RESTful service can be found here: http://<host:port>/swagger

### Build the GUI ###

--> Windows only :-/

1. git clone https://github.com/merschformann/sardine-can.git
1. Open *SardineCan.sln* with Visual Studio
1. Set *SC.GUI* as startup project
1. Compile and execute

### Gurobi & CPLEX support ###

Unfortunately, I cannot ship the Gurobi and CPLEX libraries with the code.  
I made an attempt of not relying on these during compile time by moving them to a Nuget package (Atto.LinearWrap). Even though this part works, I had some issues when supplying the dlls later on. Let me know, if you have ideas how to overcome this.

I hope I can provide a solution for all who have access to Gurobi and/or CPLEX in the future, so that the model formulations can also be tested.

### Contributors ###

The code mainly originated from the master-thesis of Marius Merschformann in 2014. Find a copy [here](./Material/MasterThesis/MasterThesis_MariusMerschformann.pdf).
The implementations around pre-processing were done by Daniel Erdmann and Simon Moss during a university project.
Further work on ALNS & some further extensions were done in collaboration with Daniela Guericke.
