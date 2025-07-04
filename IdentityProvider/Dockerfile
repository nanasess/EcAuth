# デバッグ コンテナーをカスタマイズする方法と、Visual Studio がこの Dockerfile を使用してより高速なデバッグのためにイメージをビルドする方法については、https://aka.ms/customizecontainer をご覧ください。

# このステージは、VS から高速モードで実行するときに使用されます (デバッグ構成の既定値)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# このステージは、サービス プロジェクトのビルドに使用されます
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["IdentityProvider/IdentityProvider.csproj", "IdentityProvider/"]
RUN dotnet restore "./IdentityProvider/IdentityProvider.csproj"
COPY . .
WORKDIR "/src/IdentityProvider"
RUN dotnet build "./IdentityProvider.csproj" -c $BUILD_CONFIGURATION -o /app/build

# FROM build AS migration
# ARG BUILD_CONFIGURATION=Release
# ENV PATH="$PATH:/root/.dotnet/tools"
# RUN dotnet tool install --global dotnet-ef --version 9.0.0-preview.3.24174.2 && dotnet tool restore
# RUN dotnet ef migrations bundle --output /app/publish/bundle && chmod +x /app/publish/bundle
# ENTRYPOINT ["/app/efbundle", "--connection", "Server=db;Database=EcAuthDb;User Id=SA;Password=<YourStrong@Passw0rd>;TrustServerCertificate=true;MultipleActiveResultSets=true"]

# このステージは、最終ステージにコピーするサービス プロジェクトを公開するために使用されます
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
ENV PATH="$PATH:/root/.dotnet/tools"
RUN dotnet publish "./IdentityProvider.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false \
    && dotnet tool install --global dotnet-ef --version 8.0.0 && dotnet tool restore \
    && dotnet ef migrations bundle --output /app/publish/bundle && chmod +x /app/publish/bundle

# このステージは、運用環境または VS から通常モードで実行している場合に使用されます (デバッグ構成を使用しない場合の既定)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "IdentityProvider.dll"]
