# syntax=docker/dockerfile-upstream:master-labs
ARG DOTNET_SDK_VERSION=8.0-jammy
ARG RUNTIME_CONTAINER=ghcr.io/thetote/containers/dotnet-runtime:1.0-dotnet8.0.8
FROM ${RUNTIME_CONTAINER} AS base
WORKDIR /app
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
EXPOSE 3000
EXPOSE 5030
EXPOSE 5040

# Setup nuget and restore packages
FROM --platform=${BUILDPLATFORM} mcr.microsoft.com/dotnet/sdk:${DOTNET_SDK_VERSION} AS publish
ARG BUILDPLATFORM
ARG TARGETARCH
ARG DOTNET_BUILD_PROJECT
WORKDIR /app
COPY --parents  *.sln **/*.csproj /app/
COPY <<EOF /app/nuget.config
<?xml version="1.0" encoding="UTF-8"?>
<configuration>
  <packageSources>
    <add key="public" value="https://api.nuget.org/v3/index.json" />
    <add key="totefeed" value="https://nuget.pkg.github.com/TheTote/index.json"  />
  </packageSources>
  <packageSourceCredentials>
    <totefeed>
      <add key="Username" value="%Nuget_ToteFeedUserName%" />
      <add key="ClearTextPassword" value="%Nuget_ToteFeedPassword%" />
    </totefeed>
  </packageSourceCredentials>
</configuration>
EOF
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
  --mount=type=secret,id=Nuget_ToteFeedPassword \
  --mount=type=secret,id=Nuget_ToteFeedUserName \
  export Nuget_ToteFeedPassword="$(cat /run/secrets/Nuget_ToteFeedPassword)" && \
  export Nuget_ToteFeedUserName="$(cat /run/secrets/Nuget_ToteFeedUserName)" && \
  if [ "${TARGETARCH}" = "arm64" ]; then \
    export RID="linux-arm64"; \
  else \
    export RID="linux-x64"; \
  fi && \
  dotnet restore --runtime ${RID}
COPY --parents --link ./source ./tests ./docs /app/
RUN --mount=type=cache,id=nuget,target=/root/.nuget/packages \
  if [ "${TARGETARCH}" = "arm64" ]; then \
    export RID="linux-arm64"; \
  else \
    export RID="linux-x64"; \
  fi && \
  dotnet publish \
  ${DOTNET_BUILD_PROJECT} \
  --output /out \
  --configuration Release \
  --runtime ${RID} \
  -p PublishSingleFile=true \
  --self-contained \
  --p IncludeNativeLibrariesForSelfExtract=true \
  --no-restore

#  Build the runtime image
FROM --platform=${BUILDPLATFORM} base AS final
ARG BUILDPLATFORM
ARG DOTNET_RUNTIME_NAME
USER ${APP_UID}
WORKDIR /app
COPY --from=publish /out/${DOTNET_RUNTIME_NAME} ./app
COPY --from=publish /out/appsettings.json ./appsettings.json
ENTRYPOINT ["./app"]
