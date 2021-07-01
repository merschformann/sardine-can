#!/bin/bash
docker run --restart=always -d -p 4550:80 --name sardinecan sardinecan-canary
