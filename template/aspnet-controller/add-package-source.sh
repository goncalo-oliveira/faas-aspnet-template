#!/bin/bash

# $1: package source
# $2: username
# $3: password

if [ -n "$1" ] && [ -n "$2" ] && [ -n "$3" ]; then
    echo "--source $1 --username $2 --password $3"
    # dotnet nuget add source 
    dotnet nuget add source -n private -u $2 -p $3 --store-password-in-clear-text $1
fi
