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

1. git clone https://github.com/merschformann/sardinecan.git
1. Open *SardineCan.sln* with Visual Studio
1. Set *SC.GUI* as startup project
1. Compile and execute

### Contributors ###

 The code mainly originated from the master-thesis of Marius Merschformann in 2014.
 The implementations around pre-processing were done by Daniel Erdmann and Simon Moss during a university project.
 Further work on ALNS & some further extensions were done in collaboration with Daniela Guericke.
 