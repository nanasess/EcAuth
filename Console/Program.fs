module EcAuthConsole

open System
open ConsoleAppFramework
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open BCrypt.Net
open EcAuth
open Models

type EcAuthConsoleApp(dbContext: EcAuthDbContext) =
    inherit ConsoleAppBase()

    [<Command("encrypt", "Encrypts the client secret")>]
    member _.encrypt([<Option("s")>]clientSecret: string) =
        let hashSecret = BCrypt.EnhancedHashPassword(clientSecret, 12)
        Console.WriteLine($"Client Secret: {hashSecret}")

    [<Command("create-client", "Creates a new client")>]
    member _.create_client(appName: string, [<Option("c")>]clientId: string, [<Option("s")>]clientSecret: string) =
        let hashSecret = BCrypt.EnhancedHashPassword(clientSecret, 12)
        Console.WriteLine($"Client Id: {clientId}")
        Console.WriteLine($"Client Secret: {hashSecret}")

        let client = createClient clientId hashSecret appName
        dbContext.Clients.Add(client) |> ignore
        dbContext.SaveChanges() |> ignore

let configureServices (services: IServiceCollection) =
    services.AddDbContext<EcAuthDbContext>() |> ignore
    services.AddTransient<Func<EcAuthDbContext>>(fun sp -> System.Func<_>(fun () -> sp.GetService<EcAuthDbContext>())) |> ignore

[<EntryPoint>]
let main args =
    Host.CreateDefaultBuilder()
        .ConfigureServices(configureServices)
        .RunConsoleAppFrameworkAsync<EcAuthConsoleApp>(args)
        |> Async.AwaitTask
        |> Async.RunSynchronously
    0
