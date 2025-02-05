# Visual Studio の docker compose から起動した場合のコンテナ操作

- `-p ec-auth` は [`docker-compose.dcproj`](./docker-compose.dcproj) の `<DockerComposeProjectName>` で指定している

## DB のバックアップ

``` shell
docker compose -f docker-compose.yml -f docker-compose.override.yml -f obj/Docker/docker-compose.vs.debug.g.yml -f docker-compose.vs.debug.yml -p ecauth --ansi never exec db /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P '<YourStrong@Passw0rd>' -C -Q "BACKUP DATABASE [EcAuthDb] TO DISK = N'/var/opt/mssql/backup/EcAuthDb.bak' WITH NOFORMAT, NOINIT, NAME = 'EcAuthDbBackup', SKIP, NOREWIND, NOUNLOAD, STATS = 10"
```

## DB のリストア

### バックアップファイルのパスの確認
``` shell
docker compose -f docker-compose.yml -f docker-compose.override.yml -f obj/Docker/docker-compose.vs.debug.g.yml -f docker-compose.vs.debug.yml -p ec-auth --ansi never exec db /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P '<YourStrong@Passw0rd>' -C -Q 'RESTORE FILELISTONLY FROM DISK = "/var/opt/mssql/backup/EcAuthDb.bak"'  | tr -s ' ' | cut -d ' ' -f 1-2
```

### リストア

``` shell
docker compose -f docker-compose.yml -f docker-compose.override.yml -f obj/Docker/docker-compose.vs.debug.g.yml -f docker-compose.vs.debug.yml -p ec-auth --ansi never exec db /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P '<YourStrong@Passw0rd>' -C -Q 'RESTORE DATABASE EcAuthDb FROM DISK = "/var/opt/mssql/backup/EcAuthDb.bak" WITH MOVE "EcAuthDb" TO "/var/opt/mssql/data/EcAuthDb.mdf", MOVE "EcAuthDb_log" TO "/var/opt/mssql/data/EcAuthDb_log.ldf"'
```

## See Also
- https://github.com/efcore/EFCore.FSharp/blob/master/GETTING_STARTED.md
