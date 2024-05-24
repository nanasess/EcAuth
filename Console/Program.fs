module EcAuthConsole

open System
open ConsoleAppFramework
open Microsoft.Extensions.Hosting
open BCrypt.Net

type EcAuthConsoleApp() =
    inherit ConsoleAppBase()

    member _.Console([<Option("c")>]clientId: string, [<Option("s")>]clientSecret: string) =
        let hashSecret = BCrypt.EnhancedHashPassword(clientSecret, 12)
        Console.WriteLine($"Client Id: {clientId}")
        Console.WriteLine($"Client Secret: {hashSecret}")

[<EntryPoint>]
let main args =
    Host.CreateDefaultBuilder()
        .RunConsoleAppFrameworkAsync<EcAuthConsoleApp>(args)
        |> Async.AwaitTask
        |> Async.RunSynchronously
    0
