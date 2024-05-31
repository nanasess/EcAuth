module Models

open System
open System.ComponentModel.DataAnnotations
open System.ComponentModel.DataAnnotations.Schema

[<CLIMutable>]
type Client = {
  [<Key; DatabaseGenerated(DatabaseGeneratedOption.Identity)>]
  Id: int

  [<Required; StringLength(255)>]
  ClientId: string
  [<Required; StringLength(1024)>]

  ClientSecret: string
  [<Required; StringLength(255)>]
  AppName: string
  CreatedAt: DateTime
  UpdatedAt: DateTime
}

let createClient clientId clientSecret appName =
    { Id = 0; ClientId = clientId; ClientSecret = clientSecret; AppName = appName; CreatedAt = DateTime.UtcNow; UpdatedAt = DateTime.UtcNow }
