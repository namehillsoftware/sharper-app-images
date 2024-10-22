FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build

RUN apt-get update && apt-get install -y clang zlib1g-dev

WORKDIR /src

# Copy everything
COPY SharperAppImages.sln .
COPY SharperIntegration/SharperIntegration.csproj ./SharperIntegration/
COPY SharperIntegration.Test/SharperIntegration.Test.csproj ./SharperIntegration.Test/

RUN dotnet restore

COPY . ./

RUN dotnet test

WORKDIR /src/SharperIntegration

# Restore as distinct layers
RUN dotnet restore -r linux-x64

RUN dotnet publish \
    --no-restore \
    --self-contained true \
    -p:DebugType=None \
    -p:DebugSymbols=false \
    -o /out

FROM debian:bullseye-slim

ENV DEBIAN_FRONTEND=noninteractive

RUN apt-get update -y && \
    apt-get upgrade -y && \
    apt-get install --no-install-recommends wget ca-certificates fuse file gpg appstream desktop-file-utils zsync -y && \
    wget https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage -O appimagetool && \
    chmod +x appimagetool #&&

COPY --from=build /out /AppDir/
COPY --from=build /out/SharperIntegration /AppDir/AppRun
COPY --from=build /src/AppImage /AppDir/

WORKDIR /out

ENTRYPOINT ["/appimagetool", "/AppDir", "-g"]
