FROM openfaas/of-watchdog:0.8.1 as watchdog

FROM mcr.microsoft.com/dotnet/sdk:5.0 as builder

# suppress data collection
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1

# caches restore result by copying csproj file separately
WORKDIR /source/faas/function
COPY function/function.csproj .

# restore packages
RUN dotnet restore

# Copies the rest of the code
WORKDIR /source/faas
COPY . .

# build and publish
RUN dotnet publish -c release -o published function/function.csproj

# runner
FROM docker.pkg.github.com/redpandaltd/faas-run/faas-run:0.3.1

COPY --from=watchdog /fwatchdog /usr/bin/

# Create a non-root user
RUN addgroup --system app \
    && adduser --system --ingroup app app

WORKDIR /home/app/
COPY --from=builder /source/faas/published .
RUN chown app:app -R /home/app

USER app

ENV fprocess="faas-run ./function.dll"
ENV cgi_headers="true"
ENV mode="http"
ENV upstream_url="http://localhost:9000"
EXPOSE 8080

HEALTHCHECK --interval=3s CMD [ -e /tmp/.lock ] || exit 1

CMD ["fwatchdog"]
