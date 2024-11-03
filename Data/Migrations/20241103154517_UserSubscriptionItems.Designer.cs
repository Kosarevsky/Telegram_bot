﻿// <auto-generated />
using System;
using Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Data.Migrations
{
    [DbContext(typeof(BotContext))]
    [Migration("20241103154517_UserSubscriptionItems")]
    partial class UserSubscriptionItems
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "8.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("Data.Context.BotContext+CurrentDateTimeDto", b =>
                {
                    b.Property<DateTime>("CurrentDateTime")
                        .HasColumnType("datetime2");

                    b.ToTable("CurrentDateTimeDto");
                });

            modelBuilder.Entity("Data.Entities.AvailableDate", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Code")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("Date")
                        .HasColumnType("datetime2");

                    b.Property<int>("OperationId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("OperationId");

                    b.ToTable("AvailableDate");
                });

            modelBuilder.Entity("Data.Entities.Execution", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("ExecutionDateTime")
                        .HasColumnType("datetime2");

                    b.HasKey("Id");

                    b.ToTable("Execution");
                });

            modelBuilder.Entity("Data.Entities.User", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<long>("Id"));

                    b.Property<long>("TelegramUserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.ToTable("User");
                });

            modelBuilder.Entity("Data.Entities.UserSubscription", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("SubscriptionCode")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime>("SubscriptionDateTime")
                        .HasColumnType("datetime2");

                    b.Property<long>("UserId")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("UserSubscription");
                });

            modelBuilder.Entity("Data.Entities.UserSubscriptionItems", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("AvailableDate")
                        .HasColumnType("datetime2");

                    b.Property<int>("UserSubscriptionId")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.HasIndex("UserSubscriptionId");

                    b.ToTable("UserSubscriptionItems");
                });

            modelBuilder.Entity("Data.Entities.AvailableDate", b =>
                {
                    b.HasOne("Data.Entities.Execution", "Execution")
                        .WithMany("AvailableDates")
                        .HasForeignKey("OperationId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Execution");
                });

            modelBuilder.Entity("Data.Entities.UserSubscription", b =>
                {
                    b.HasOne("Data.Entities.User", "User")
                        .WithMany("Subscriptions")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("Data.Entities.UserSubscriptionItems", b =>
                {
                    b.HasOne("Data.Entities.UserSubscription", "UserSubscription")
                        .WithMany("UserSubscriptionItems")
                        .HasForeignKey("UserSubscriptionId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("UserSubscription");
                });

            modelBuilder.Entity("Data.Entities.Execution", b =>
                {
                    b.Navigation("AvailableDates");
                });

            modelBuilder.Entity("Data.Entities.User", b =>
                {
                    b.Navigation("Subscriptions");
                });

            modelBuilder.Entity("Data.Entities.UserSubscription", b =>
                {
                    b.Navigation("UserSubscriptionItems");
                });
#pragma warning restore 612, 618
        }
    }
}
