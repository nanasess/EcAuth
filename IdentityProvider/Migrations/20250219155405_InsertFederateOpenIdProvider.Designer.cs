﻿// <auto-generated />
using System;
using IdentityProvider.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace IdentityProvider.Migrations
{
    [DbContext(typeof(EcAuthDbContext))]
    [Migration("20250219155405_InsertFederateOpenIdProvider")]
    partial class InsertFederateOpenIdProvider
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.12")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("IdentityProvider.Models.Account", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset")
                        .HasColumnName("created_at");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("email");

                    b.Property<string>("Password")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("password");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("datetimeoffset")
                        .HasColumnName("updated_at");

                    b.HasKey("Id");

                    b.ToTable("account");
                });

            modelBuilder.Entity("IdentityProvider.Models.Client", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("AppName")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("app_name");

                    b.Property<string>("ClientId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("client_id");

                    b.Property<string>("ClientSecret")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("client_secret");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset")
                        .HasColumnName("created_at");

                    b.Property<int?>("OrganizationId")
                        .HasColumnType("int")
                        .HasColumnName("organization_id");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("datetimeoffset")
                        .HasColumnName("updated_at");

                    b.HasKey("Id");

                    b.HasIndex("OrganizationId");

                    b.ToTable("client");
                });

            modelBuilder.Entity("IdentityProvider.Models.OpenIdProvider", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("AuthorizationEndpoint")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("authorization_endpoint");

                    b.Property<int?>("ClientId")
                        .HasColumnType("int")
                        .HasColumnName("client_id");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset")
                        .HasColumnName("created_at");

                    b.Property<string>("DiscoveryDocumentUri")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("discovery_document_uri");

                    b.Property<string>("IdpClientId")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("idp_client_id");

                    b.Property<string>("IdpClientSecret")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("idp_client_secret");

                    b.Property<string>("Issuer")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("issuer");

                    b.Property<string>("JwksUri")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("jwks_uri");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("name");

                    b.Property<string>("TokenEndpoint")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("token_endpoint");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("datetimeoffset")
                        .HasColumnName("updated_at");

                    b.Property<string>("UserinfoEndpoint")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("userinfo_endpoint");

                    b.HasKey("Id");

                    b.HasIndex("ClientId");

                    b.ToTable("open_id_provider");
                });

            modelBuilder.Entity("IdentityProvider.Models.OpenIdProviderScope", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset")
                        .HasColumnName("created_at");

                    b.Property<bool>("IsEnabled")
                        .HasColumnType("bit")
                        .HasColumnName("is_enabled");

                    b.Property<int>("OpenIdProviderId")
                        .HasColumnType("int")
                        .HasColumnName("open_id_provider_id");

                    b.Property<string>("Scope")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("scope");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("datetimeoffset")
                        .HasColumnName("updated_at");

                    b.HasKey("Id");

                    b.HasIndex("OpenIdProviderId");

                    b.ToTable("open_id_provider_scope");
                });

            modelBuilder.Entity("IdentityProvider.Models.Organization", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("code");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset")
                        .HasColumnName("created_at");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("name");

                    b.Property<string>("TenantName")
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("tenant_name");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("datetimeoffset")
                        .HasColumnName("updated_at");

                    b.HasKey("Id");

                    b.ToTable("organization");
                });

            modelBuilder.Entity("IdentityProvider.Models.RedirectUri", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("ClientId")
                        .HasColumnType("int")
                        .HasColumnName("client_id");

                    b.Property<DateTimeOffset>("CreatedAt")
                        .HasColumnType("datetimeoffset")
                        .HasColumnName("created_at");

                    b.Property<DateTimeOffset>("UpdatedAt")
                        .HasColumnType("datetimeoffset")
                        .HasColumnName("updated_at");

                    b.Property<string>("Uri")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("uri");

                    b.HasKey("Id");

                    b.HasIndex("ClientId");

                    b.ToTable("redirect_uri");
                });

            modelBuilder.Entity("IdentityProvider.Models.RsaKeyPair", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("ClientId")
                        .HasColumnType("int")
                        .HasColumnName("client_id");

                    b.Property<string>("PrivateKey")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("private_key");

                    b.Property<string>("PublicKey")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("public_key");

                    b.HasKey("Id");

                    b.HasIndex("ClientId")
                        .IsUnique();

                    b.ToTable("rsa_key_pair");
                });

            modelBuilder.Entity("IdentityProvider.Models.Client", b =>
                {
                    b.HasOne("IdentityProvider.Models.Organization", "Organization")
                        .WithMany("Clients")
                        .HasForeignKey("OrganizationId");

                    b.Navigation("Organization");
                });

            modelBuilder.Entity("IdentityProvider.Models.OpenIdProvider", b =>
                {
                    b.HasOne("IdentityProvider.Models.Client", "Client")
                        .WithMany("OpenIdProviders")
                        .HasForeignKey("ClientId");

                    b.Navigation("Client");
                });

            modelBuilder.Entity("IdentityProvider.Models.OpenIdProviderScope", b =>
                {
                    b.HasOne("IdentityProvider.Models.OpenIdProvider", "OpenIdProvider")
                        .WithMany("Scopes")
                        .HasForeignKey("OpenIdProviderId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("OpenIdProvider");
                });

            modelBuilder.Entity("IdentityProvider.Models.RedirectUri", b =>
                {
                    b.HasOne("IdentityProvider.Models.Client", "Client")
                        .WithMany("RedirectUris")
                        .HasForeignKey("ClientId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Client");
                });

            modelBuilder.Entity("IdentityProvider.Models.RsaKeyPair", b =>
                {
                    b.HasOne("IdentityProvider.Models.Client", "Client")
                        .WithOne("RsaKeyPair")
                        .HasForeignKey("IdentityProvider.Models.RsaKeyPair", "ClientId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Client");
                });

            modelBuilder.Entity("IdentityProvider.Models.Client", b =>
                {
                    b.Navigation("OpenIdProviders");

                    b.Navigation("RedirectUris");

                    b.Navigation("RsaKeyPair");
                });

            modelBuilder.Entity("IdentityProvider.Models.OpenIdProvider", b =>
                {
                    b.Navigation("Scopes");
                });

            modelBuilder.Entity("IdentityProvider.Models.Organization", b =>
                {
                    b.Navigation("Clients");
                });
#pragma warning restore 612, 618
        }
    }
}
