FROM mcr.microsoft.com/dotnet/sdk:8.0@sha256:35792ea4ad1db051981f62b313f1be3b46b1f45cadbaa3c288cd0d3056eefb83

RUN apt-get update && apt-get install -y clang zlib1g-dev

WORKDIR /src

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore -r linux-x64
 
ENTRYPOINT ["dotnet"]