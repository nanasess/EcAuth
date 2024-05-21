FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App

# Copy everything
COPY ./IdP /App
COPY ./.paket /.paket
COPY ./.config /.config
COPY ./paket.lock ./
COPY ./paket.dependencies ./
# Restore as distinct layers
RUN dotnet tool restore
RUN dotnet restore
RUN dotnet paket restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "IdP.dll"]
