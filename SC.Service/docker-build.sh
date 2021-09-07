#!/bin/bash
docker build -f Dockerfile .. --network=host -t sardinecan:latest
