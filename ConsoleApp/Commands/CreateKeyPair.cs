using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ConsoleAppFramework;
using IdentityProvider;
using Microsoft.EntityFrameworkCore;

namespace EcAuthConsoleApp.Commands
{
    [RegisterCommands]
    internal class CreateKeyPair(EcAuthDbContext context)
    {
        public int Create(string Id)
        {
            var client = context.Clients.Where(c => c.ClientId == Id).FirstOrDefault();
            if (client == null)
            {
                Console.WriteLine("Client not found");
                return 1;
            }
            using RSA rsa = RSA.Create();
            var keyPair = context.RsaKeyPairs.Where(r => r.ClientId == client.Id).FirstOrDefault();
            if (keyPair != null)
            {
                Console.WriteLine("キーペアが既に存在するため再生成します...");
                context.RsaKeyPairs.Remove(keyPair);
            }
            client.RsaKeyPair = new IdentityProvider.Models.RsaKeyPair
            {
                PublicKey = rsa.ExportRSAPublicKeyPem(),
                PrivateKey = rsa.ExportRSAPrivateKeyPem()
            };
            context.SaveChanges();
            // Console.WriteLine(client.RsaKeyPair.PublicKey);
            // Console.WriteLine(client.RsaKeyPair.PrivateKey);
            Console.WriteLine($"{client.ClientId} のキーペアを生成しました");

            return 0;
        }
    }
}
