﻿// <auto-generated />
using System;
using FriendZoneHub.Server.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace FrirendZoneHub.Server.Migrations
{
    [DbContext(typeof(ChatAppContext))]
    [Migration("20241007141021_ConfigureChatRoomUserRelationships")]
    partial class ConfigureChatRoomUserRelationships
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            MySqlModelBuilderExtensions.AutoIncrementColumns(modelBuilder);

            modelBuilder.Entity("FriendZoneHub.Server.Models.ChatRoom", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<bool>("IsPrivate")
                        .HasColumnType("tinyint(1)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.ToTable("ChatRooms");
                });

            modelBuilder.Entity("FriendZoneHub.Server.Models.Message", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int>("ChatRoomId")
                        .HasColumnType("int");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("UserId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("ChatRoomId");

                    b.HasIndex("UserId");

                    b.ToTable("Messages");
                });

            modelBuilder.Entity("User", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    MySqlPropertyBuilderExtensions.UseMySqlIdentityColumn(b.Property<int>("Id"));

                    b.Property<int?>("ChatRoomId")
                        .HasColumnType("int");

                    b.Property<int?>("ChatRoomId1")
                        .HasColumnType("int");

                    b.Property<string>("Email")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("PasswordHash")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.Property<string>("Username")
                        .IsRequired()
                        .HasColumnType("longtext");

                    b.HasKey("Id");

                    b.HasIndex("ChatRoomId");

                    b.HasIndex("ChatRoomId1");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("FriendZoneHub.Server.Models.Message", b =>
                {
                    b.HasOne("FriendZoneHub.Server.Models.ChatRoom", "ChatRoom")
                        .WithMany("Messages")
                        .HasForeignKey("ChatRoomId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("User", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ChatRoom");

                    b.Navigation("User");
                });

            modelBuilder.Entity("User", b =>
                {
                    b.HasOne("FriendZoneHub.Server.Models.ChatRoom", null)
                        .WithMany("AllowedUsers")
                        .HasForeignKey("ChatRoomId");

                    b.HasOne("FriendZoneHub.Server.Models.ChatRoom", null)
                        .WithMany("Users")
                        .HasForeignKey("ChatRoomId1");
                });

            modelBuilder.Entity("FriendZoneHub.Server.Models.ChatRoom", b =>
                {
                    b.Navigation("AllowedUsers");

                    b.Navigation("Messages");

                    b.Navigation("Users");
                });
#pragma warning restore 612, 618
        }
    }
}