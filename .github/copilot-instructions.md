## 概要

このプロジェクトは、 OpenID Connect の ID フェデレーションに特化した Identity Provider を構築します。

### グランドルール

- 日本語でやりとりをお願いします。

### プロジェクト構成

.NET Core 8.0 を使用します。
E2Eテストは Playwright を使用します。

- [IdentityProvider](../IdentityProvider): IdP の API を提供するプロジェクト
- [IdpUtilities](../IdpUtilities): 各プロジェクト共通のユーティリティを提供するプロジェクト
- [ConsoleApp](../ConsoleApp): コマンドラインユーティリティ
- [MockOpenIdProvider](../MockOpenIdProvider): E2Eテストで使用するための IdP
- [E2ETests](../E2ETests): IdentityProvider の E2Eテスト
- [IdpUtilities.Test](../IdpUtilities.Test) IdpUtilities のユニットテスト

### データベース

- Microsoft SQL Server 2022 を使用します
- EntityFramework Core 8.0 を使用します

## 開発環境

WSL2上の docker compose を使用し、Linux コンテナを使用します。以下のようにして起動可能です。

``` shell
docker compose -f .\docker-compose.yml -f .\docker-compose.override.yml -f .\obj\Docker\docker-compose.vs.debug.g.yml -f .\docker-compose.vs.debug.yml  -p ec-auth up -d
```
