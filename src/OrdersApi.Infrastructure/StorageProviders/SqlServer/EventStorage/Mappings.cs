using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using OrdersApi.Infrastructure.StorageProviders.SqlServer.EventLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage
{
    [ExcludeFromCodeCoverage]
    public class EventDataMapping : IEntityTypeConfiguration<EventData>
    {
        public void Configure(EntityTypeBuilder<EventData> builder)
        {
            builder.ToTable("EventData");
            builder.HasKey(x => x.EventKey);
            builder.Ignore(x => x.SagaProcessKey);
            builder.Property(x => x.ClassVersion).IsRequired().HasColumnType("tinyint");
            builder.Property(x => x.TimesSent).IsRequired().HasColumnType("tinyint");
            builder.Property(x => x.State).IsRequired().HasColumnType("tinyint");
            builder.Property(x => x.TargetVersion).IsRequired().HasColumnType("smallint");
            builder.Property(x => x.EventCommittedTimestamp).IsRequired().HasColumnType("datetime2(7)");
            builder.Property(x => x.ApplicationKey).IsRequired().HasColumnType("char(25)");
            builder.Property(x => x.EventKey).IsRequired().HasColumnType("char(25)");
            builder.Property(x => x.AggregateKey).IsRequired().HasColumnType("char(25)");
            builder.Property(x => x.CorrelationKey).IsRequired().HasColumnType("char(32)");
            builder.HasIndex(p => p.AggregateKey);
            builder.HasIndex(p => new { p.ApplicationKey, p.CorrelationKey }).IsUnique();
            builder.Property(x => x.Payload).IsRequired();
        }
    }
     
}
