#!/bin/bash

# $1: package source
# $2: username
# $3: password

if [ -n "$1" ] && [ -n "$2" ] && [ -n "$3" ]; then
    # dotnet nuget add source with username and password
    dotnet nuget add source -n private -u $2 -p $3 --store-password-in-clear-text $1
elif [ -n "$1" ]; then
    # dotnet nuget add source
    dotnet nuget add source -n private $1
fi
