namespace EcAuth

open System.ComponentModel.DataAnnotations
open Microsoft.EntityFrameworkCore
open EntityFrameworkCore.FSharp.Extensions
open Models

type EcAuthDbContext() =
    inherit DbContext()

    [<DefaultValue>]
    val mutable _clients: DbSet<Client>

    member this.Clients
        with get() = this._clients
        and set(value) = this._clients <- value

    override _.OnConfiguring(optionsBuilder: DbContextOptionsBuilder) =
        optionsBuilder.UseSqlServer("Server=127.0.0.1;Database=EcAuthDb;User Id=SA;Password=<YourStrong@Passw0rd>;TrustServerCertificate=true;MultipleActiveResultSets=true") |> ignore

    override _.OnModelCreating(modelBuilder: ModelBuilder) =
        modelBuilder.RegisterOptionTypes() |> ignore
