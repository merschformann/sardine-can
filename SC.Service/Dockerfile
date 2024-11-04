# >>> 1st stage: build the application
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG TARGETARCH
ARG BUILDPLATFORM

WORKDIR /src
COPY . .
RUN dotnet restore "SC.Service/SC.Service.csproj"
WORKDIR "/src/SC.Service"
RUN dotnet build "SC.Service.csproj" -c Release -o /app/build -a $TARGETARCH

# >>> 2nd stage: publish the application
FROM build AS publish
RUN dotnet publish "SC.Service.csproj" -c Release -o /app/publish \
    --self-contained true \
    /p:PublishSingleFile=true \
    -a $TARGETARCH

# >>> 3rd stage: create the final image
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/runtime-deps:8.0-alpine AS final
LABEL org.opencontainers.image.source="https://github.com/merschformann/sardine-can"
ARG TARGETARCH
ARG BUILDPLATFORM
ENV ASPNETCORE_HTTP_PORTS=80
EXPOSE 80
WORKDIR /app

# create a new user and change directory ownership
RUN adduser --disabled-password \
    --home /app \
    --gecos '' dotnetuser && chown -R dotnetuser /app

# impersonate into the new user
USER dotnetuser

# copy the published files and configure the entrypoint
COPY --from=publish /app/publish .
ENTRYPOINT ["./SC.Service"]
