#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["SC.Service/SC.Service.csproj", "SC.Service/"]
RUN dotnet restore "SC.Service/SC.Service.csproj"
COPY . .
WORKDIR "/src/SC.Service"
RUN dotnet build "SC.Service.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SC.Service.csproj" -c Release -o /app/publish

FROM base AS final
LABEL org.opencontainers.image.source="https://github.com/merschformann/sardine-can"
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SC.Service.dll"]
