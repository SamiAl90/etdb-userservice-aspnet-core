﻿// <auto-generated />

using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Etdb.UserService.Data.Migrations
{
    [DbContext(typeof(UserServiceContext))]
    partial class UserServiceContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.0.1-rtm-125")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("ETDB.API.UserService.Domain.Entities.Securityrole", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Description");

                    b.Property<string>("Designation")
                        .IsRequired();

                    b.Property<bool>("IsSystem");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                    b.HasKey("Id");

                    b.HasIndex("Designation")
                        .IsUnique();

                    b.ToTable("Securityroles");
                });

            modelBuilder.Entity("ETDB.API.UserService.Domain.Entities.User", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Email");

                    b.Property<string>("LastName");

                    b.Property<string>("Name");

                    b.Property<string>("Password");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                    b.Property<byte[]>("Salt")
                        .IsRequired();

                    b.Property<string>("UserName");

                    b.HasKey("Id");

                    b.HasIndex("Email")
                        .IsUnique()
                        .HasFilter("[Email] IS NOT NULL");

                    b.HasIndex("UserName")
                        .IsUnique()
                        .HasFilter("[UserName] IS NOT NULL");

                    b.ToTable("Users");
                });

            modelBuilder.Entity("ETDB.API.UserService.Domain.Entities.UserSecurityrole", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken()
                        .ValueGeneratedOnAddOrUpdate();

                    b.Property<Guid>("SecurityroleId");

                    b.Property<Guid>("UserId");

                    b.HasKey("Id");

                    b.HasIndex("SecurityroleId");

                    b.HasIndex("UserId");

                    b.ToTable("UserSecurityroles");
                });

            modelBuilder.Entity("ETDB.API.UserService.Domain.Entities.UserSecurityrole", b =>
                {
                    b.HasOne("ETDB.API.UserService.Domain.Entities.Securityrole", "Securityrole")
                        .WithMany("UserSecurityroles")
                        .HasForeignKey("SecurityroleId")
                        .OnDelete(DeleteBehavior.Restrict);

                    b.HasOne("ETDB.API.UserService.Domain.Entities.User", "User")
                        .WithMany("UserSecurityroles")
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Restrict);
                });
#pragma warning restore 612, 618
        }
    }
}