#!/bin/bash

# $1: secret-name

secretFilePath="/run/secrets/$1"

if [ -n "$1" ] && [ -f "$secretFilePath" ]; then
    xmllint --xpath "//configuration/packageSources/add" $secretFilePath | \
    while read -r xmlElement; do
        pkgSource=$(echo $xmlElement | awk -F\" '{ print $2 }')

        if [ "$pkgSource" = "nuget.org" ]; then
            # skip nuget.org
            continue
        fi

        pkgSourceUrl=$(echo $xmlElement | awk -F\" '{ print $4 }')
        pkgSourceUsername=$(xmllint --xpath "string(//configuration/packageSourceCredentials/$pkgSource/add[@key='Username']/@value)" $secretFilePath)
        pkgSourcePassword=$(xmllint --xpath "string(//configuration/packageSourceCredentials/$pkgSource/add[@key='ClearTextPassword']/@value)" $secretFilePath)

        if [ -n "$pkgSourceUsername" ] && [ -n "$pkgSourcePassword" ]; then
            # dotnet nuget add source with username and password
            dotnet nuget add source -n $pkgSource \
            -u $pkgSourceUsername \
            -p $pkgSourcePassword \
            --store-password-in-clear-text $pkgSourceUrl
        else
            # dotnet nuget add source
            dotnet nuget add source -n $pkgSource $pkgSourceUrl
        fi

        echo "added '$pkgSource' package source"
    done
fi
