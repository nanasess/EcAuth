# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## リポジトリ構成

このプロジェクトは以下の2つのリポジトリで構成されています：

- **./**: アプリケーションコードのみを管理
- **docs/**: 全てのドキュメント、設計書、ガイドを管理

Claude Code Actions の制限で `working_directory` を変更できないため、このディレクトリ内に EcAuthDocs を clone しています。

## 作業時の注意点

- ドキュメント関連の作業は docs/ で実行
- コード関連の作業はこのリポジトリで実行
- 両リポジトリ間の整合性を保つ
- 日本語で回答してください
- 起動時に EcAuthDocs の内容を最新の main ブランチに更新してください

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

詳細なアーキテクチャ情報や開発ガイドラインは @docs/CLAUDE.md を参照してください。
