FROM mcr.microsoft.com/dotnet/sdk:10.0.403
RUN apt-get update \
    && apt-get install -y --no-install-recommends python3 python3-venv \
    && rm -rf /var/lib/apt/lists/*
ENV DOTNET_NOLOGO=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
RUN mkdir -p /bench /data && chown app:app /bench /data
WORKDIR /bench
USER app

COPY --chown=app:app scripts/ensure-sdk11.sh scripts/ensure-python.sh scripts/
COPY --chown=app:app analysis/requirements.txt analysis/
RUN ./scripts/ensure-sdk11.sh && ./scripts/ensure-python.sh

COPY --chown=app:app . .

VOLUME /data
ENTRYPOINT ["./scripts/run-rounds.sh", "--data", "/data"]
CMD ["--runs", "5"]
