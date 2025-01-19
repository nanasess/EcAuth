## DB のバックアップ

``` shell
/opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P '<YourStrong@Passw0rd>' -C -Q "BACKUP DATABASE [EcAuthDb] TO DISK = N'/var/opt/mssql/backup/EcAuthDb.bak' WITH NOFORMAT, NOINIT, NAME = 'EcAuthDbBackup', SKIP, NOREWIND, NOUNLOAD, STATS = 10"
```

## DB のリストア

### バックアップファイルのパスの確認
``` shell
/opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P '<YourStrong@Passw0rd>' -C -Q 'RESTORE FILELISTONLY FROM DISK = "/var/opt/mssql/backup/EcAuthDb.bak"'  | tr -s ' ' | cut -d ' ' -f 1-2
```

### リストア

``` shell
/opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P '<YourStrong@Passw0rd>' -C -Q 'RESTORE DATABASE EcAuthDb FROM DISK = "/var/opt/mssql/backup/EcAuthDb.bak" WITH MOVE "EcAuthDb" TO "/var/opt/mssql/data/EcAuthDb.mdf", MOVE "EcAuthDb_log" TO "/var/opt/mssql/data/EcAuthDb_log.ldf"'
```

## See Also
- https://github.com/efcore/EFCore.FSharp/blob/master/GETTING_STARTED.md
