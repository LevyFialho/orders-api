using System; 
using Microsoft.EntityFrameworkCore.Migrations;

namespace OrdersApi.Infrastructure.Migrations
{ 
    public partial class SqlServerEventStore : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"CREATE TABLE [EventsToDispatch] (
                    [TargetVersion]           SMALLINT        NOT NULL,
                    [EventKey]                CHAR (32)      NOT NULL,
                    [CorrelationKey]          CHAR (32)      NOT NULL,
                    [AggregateKey]            CHAR (32)      NOT NULL,
                    [ApplicationKey]          CHAR (32)      NOT NULL,
                    [EventCommittedTimestamp] DATETIME2 (7)  NOT NULL,
                    [ClassVersion]            TINYINT        NOT NULL, 
                    [Payload]                 NVARCHAR (MAX) NOT NULL,
                    [TimesSent]               TINYINT        NOT NULL,
                    [State]                   TINYINT        NOT NULL  CONSTRAINT CK_EventsToDispatch CHECK ([State] = 0),
                    CONSTRAINT [PK_EventsToDispatch] PRIMARY KEY CLUSTERED ([EventKey] ASC, [State] ASC)
                );

 
                CREATE UNIQUE NONCLUSTERED INDEX [IX_EventsToDispatch_ApplicationKey_CorrelationKey]
                    ON [EventsToDispatch]([ApplicationKey] ASC, [CorrelationKey] ASC);

 
                CREATE NONCLUSTERED INDEX [IX_EventsToDispatch_AggregateKey]
                    ON [EventsToDispatch]([AggregateKey] ASC);

                CREATE TABLE [EventsDispatched] (
                    [TargetVersion]           SMALLINT        NOT NULL,
                    [EventKey]                CHAR (32)      NOT NULL,
                    [CorrelationKey]          CHAR (32)      NOT NULL,
                    [AggregateKey]            CHAR (32)      NOT NULL,
                    [ApplicationKey]          CHAR (32)      NOT NULL,
                    [EventCommittedTimestamp] DATETIME2 (7)  NOT NULL,
                    [ClassVersion]            TINYINT        NOT NULL, 
                    [Payload]                 NVARCHAR (MAX) NOT NULL,
                    [TimesSent]               TINYINT        NOT NULL,
                    [State]                   TINYINT        NOT NULL  CONSTRAINT CK_EventsDispatched CHECK ([State] = 1),
                    CONSTRAINT [PK_EventsDispatched] PRIMARY KEY CLUSTERED ([EventKey] ASC, [State] ASC)
                );

 
                CREATE UNIQUE NONCLUSTERED INDEX [IX_EventsDispatched_ApplicationKey_CorrelationKey]
                    ON [EventsDispatched]([ApplicationKey] ASC, [CorrelationKey] ASC);

 
                CREATE NONCLUSTERED INDEX [IX_EventsDispatched_AggregateKey]
                    ON [EventsDispatched]([AggregateKey] ASC);");

            migrationBuilder.Sql(@"

                CREATE VIEW [EventData]
                WITH SCHEMABINDING
                AS
                SELECT [TargetVersion],[EventKey],[CorrelationKey],[AggregateKey],[ApplicationKey],
                [EventCommittedTimestamp], [ClassVersion], [Payload], [TimesSent], [State]
                 FROM [EventsDispatched]
                UNION ALL
                SELECT [TargetVersion],[EventKey],[CorrelationKey],[AggregateKey],[ApplicationKey],
                [EventCommittedTimestamp], [ClassVersion], [Payload], [TimesSent], [State]
                 FROM [EventsToDispatch]");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                                    DROP VIEW [EventData] 
                                    DROP TABLE [EventsDispatched]
                                    DROP TABLE [EventsToDispatch]");
        }
    }
}
