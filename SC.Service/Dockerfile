FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "SC.Service/SC.Service.csproj"
WORKDIR "/src/SC.Service"
RUN dotnet build "SC.Service.csproj" -c Release -o /app/build
RUN dotnet publish "SC.Service.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
LABEL org.opencontainers.image.source="https://github.com/merschformann/sardine-can"
WORKDIR /app
EXPOSE 80
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SC.Service.dll"]
