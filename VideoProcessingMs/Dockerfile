# 1️⃣ Runtime base (onde a aplicação roda)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

# Instala ffmpeg
RUN apt-get update \
    && apt-get install -y ffmpeg \
    && rm -rf /var/lib/apt/lists/*

# 2️⃣ Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia solution e projetos primeiro (melhor cache)
COPY *.sln ./
COPY Core/*.csproj ./Core/
COPY Infra.Data.SqlServer/*.csproj ./Infra.Data.SqlServer/
COPY Infra.Messaging/*.csproj ./Infra.Messaging/
COPY Infra.ObjectStorageService/*.csproj ./Infra.ObjectStorageService/
COPY Test/*.csproj ./Test/
COPY VideoProcessorWorker/*.csproj ./VideoProcessorWorker/

# Restore dependências
RUN dotnet restore

# Copia restante do código
COPY . .

# Build
WORKDIR /src/VideoProcessorWorker
RUN dotnet build -c Release -o /app/build

# 3️⃣ Publish
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# 4️⃣ Runtime final
FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "VideoProcessorWorker.dll"]
