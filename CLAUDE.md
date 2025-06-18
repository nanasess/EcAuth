# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 概要

OpenID Connect の ID フェデレーションに特化した Identity Provider システムです。

## 開発コマンド

```bash
# 環境起動
docker compose -f docker-compose.yml -f docker-compose.override.yml -f obj/Docker/docker-compose.vs.debug.g.yml -f docker-compose.vs.debug.yml -p ec-auth up -d

# ビルド
dotnet build EcAuth.sln

# テスト実行
dotnet test IdpUtilities.Test/IdpUtilities.Test.csproj
dotnet test MockOpenIdProvider.Test/MockOpenIdProvider.Test.csproj

# E2Eテスト（E2ETestsディレクトリで）
yarn install
npx playwright test
```

## 詳細情報

詳細なアーキテクチャ情報や開発ガイドラインは `../EcAuthDocs/CLAUDE.md` を参照してください。