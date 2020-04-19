#!/bin/bash
sudo docker build -f Dockerfile .. --network=host -t sardinecan-canary
