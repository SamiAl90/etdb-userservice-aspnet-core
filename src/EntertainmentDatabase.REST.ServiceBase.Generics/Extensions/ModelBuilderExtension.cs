﻿using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using IEntity = EntertainmentDatabase.REST.ServiceBase.Generics.Abstractions.IEntity;

namespace EntertainmentDatabase.REST.ServiceBase.Generics.Extensions
{
    public static class ModelBuilderExtension
    {
        public static void SetGuidAsPrimaryKey<T>(this ModelBuilder modelBuilder) where T : class, IEntity, new()
        {
            modelBuilder.Entity<T>(builder =>
            {
                builder.HasKey(entity => entity.Id);

                builder.Property(entity => entity.Id)
                    .ForSqlServerHasDefaultValueSql("newid()");
            });
        }

        public static void SupressCascadeDelete(this ModelBuilder modelBuilder)
        {
            foreach (var relation in modelBuilder.Model.GetEntityTypes().SelectMany(entity => entity.GetForeignKeys()))
            {
                relation.DeleteBehavior = DeleteBehavior.Restrict;
            }
        }
    }
}
