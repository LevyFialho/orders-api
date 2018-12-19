using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using OrdersApi.Infrastructure.StorageProviders.SqlServer.EventLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Configuration;

namespace OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage
{
    [ExcludeFromCodeCoverage]
    public class SqlEventStorageContext : DbContext
    {
        public SqlEventStorageContext(DbContextOptions<SqlEventStorageContext> options)
            : base(options)
        { }

        public virtual DbSet<EventData> Events { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new EventDataMapping());
            base.OnModelCreating(modelBuilder);
        }

         
    }
}
