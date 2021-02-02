FROM openfaas/of-watchdog:0.8.1 as watchdog

FROM mcr.microsoft.com/dotnet/sdk:5.0 as builder
ARG PACKAGE_SOURCE
ARG PACKAGE_SOURCE_USERNAME
ARG PACKAGE_SOURCE_PASSWORD

# suppress data collection
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1

# caches restore result by copying csproj file separately
WORKDIR /source/faas/api
COPY api/api.csproj .

# restore packages
COPY add-package-source.sh /tmp/
RUN chmod u+x /tmp/add-package-source.sh \
    && /tmp/add-package-source.sh $PACKAGE_SOURCE $PACKAGE_SOURCE_USERNAME $PACKAGE_SOURCE_PASSWORD \
    && dotnet restore

# Copies the rest of the code
WORKDIR /source/faas
COPY . .

# build and publish
RUN dotnet publish -c release -o published api/api.csproj

# runner
FROM docker.pkg.github.com/goncalo-oliveira/faas-api-run/faas-api-run:0.2

COPY --from=watchdog /fwatchdog /usr/bin/

# Create a non-root user
RUN addgroup --system app \
    && adduser --system --ingroup app app

WORKDIR /home/app/
COPY --from=builder /source/faas/published .
RUN chown app:app -R /home/app

USER app

ENV fprocess="faas-api-run ./api.dll"
ENV cgi_headers="true"
ENV mode="http"
ENV upstream_url="http://localhost:9000"
EXPOSE 8080

HEALTHCHECK --interval=3s CMD [ -e /tmp/.lock ] || exit 1

CMD ["fwatchdog"]
