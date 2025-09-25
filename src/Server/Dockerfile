FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files for restore (flattened layout)
COPY src/Server/ScrumPoker.Server.csproj Server/
COPY src/Client/ScrumPoker.Client.csproj Client/
COPY src/Domain/ScrumPoker.Domain.csproj Domain/
COPY src/Application/ScrumPoker.Application.csproj Application/
COPY src/Infrastructure/ScrumPoker.Infrastructure.csproj Infrastructure/
COPY src/Contracts/ScrumPoker.Contracts.csproj Contracts/
RUN dotnet restore Server/ScrumPoker.Server.csproj

# Copy the entire source
COPY src/Server Server/
COPY src/Client Client/
COPY src/Domain Domain/
COPY src/Application Application/
COPY src/Infrastructure Infrastructure/
COPY src/Contracts Contracts/
RUN dotnet build Server/ScrumPoker.Server.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish Server/ScrumPoker.Server.csproj -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ScrumPoker.Server.dll"]
