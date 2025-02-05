﻿// <auto-generated />
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MockOpenIdProvider.Models;

#nullable disable

namespace MockOpenIdProvider.Migrations
{
    [DbContext(typeof(IdpDbContext))]
    [Migration("20250205142811_InitialMigration")]
    partial class InitialMigration
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.1");

            modelBuilder.Entity("MockOpenIdProvider.Models.AccessToken", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.Property<int>("ClientId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("accessToken")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("access_token");

                    b.Property<string>("clientId")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("client_id");

                    b.Property<string>("createdAt")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("created_at");

                    b.Property<int>("expiresIn")
                        .HasColumnType("INTEGER")
                        .HasColumnName("expires_in");

                    b.HasKey("Id");

                    b.HasIndex("ClientId");

                    b.ToTable("access_token");
                });

            modelBuilder.Entity("MockOpenIdProvider.Models.AuthorizationCode", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.Property<int>("ClientId")
                        .HasColumnType("INTEGER");

                    b.Property<string>("clientId")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("client_id");

                    b.Property<string>("code")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("code");

                    b.Property<string>("createdAt")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("created_at");

                    b.Property<int>("expiresIn")
                        .HasColumnType("INTEGER")
                        .HasColumnName("expires_in");

                    b.Property<bool>("used")
                        .HasColumnType("INTEGER")
                        .HasColumnName("used");

                    b.HasKey("Id");

                    b.HasIndex("ClientId");

                    b.ToTable("authorization_code");
                });

            modelBuilder.Entity("MockOpenIdProvider.Models.Client", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER")
                        .HasColumnName("id");

                    b.Property<string>("clientId")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("client_id");

                    b.Property<string>("clientName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("client_name");

                    b.Property<string>("clientSecret")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("client_secret");

                    b.Property<string>("redirectUri")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("redirect_uri");

                    b.HasKey("Id");

                    b.ToTable("client");
                });

            modelBuilder.Entity("MockOpenIdProvider.Models.AccessToken", b =>
                {
                    b.HasOne("MockOpenIdProvider.Models.Client", "Client")
                        .WithMany()
                        .HasForeignKey("ClientId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Client");
                });

            modelBuilder.Entity("MockOpenIdProvider.Models.AuthorizationCode", b =>
                {
                    b.HasOne("MockOpenIdProvider.Models.Client", "Client")
                        .WithMany()
                        .HasForeignKey("ClientId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Client");
                });
#pragma warning restore 612, 618
        }
    }
}
