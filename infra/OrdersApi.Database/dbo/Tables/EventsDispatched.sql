CREATE TABLE [dbo].[EventsDispatched] (
    [TargetVersion]           SMALLINT       NOT NULL,
    [EventKey]                CHAR (32)      NOT NULL,
    [CorrelationKey]          CHAR (32)      NOT NULL,
    [AggregateKey]            CHAR (32)      NOT NULL,
    [ApplicationKey]          CHAR (32)      NOT NULL,
    [EventCommittedTimestamp] DATETIME2 (7)  NOT NULL,
    [ClassVersion]            TINYINT        NOT NULL,
    [Payload]                 NVARCHAR (MAX) NOT NULL,
    [TimesSent]               TINYINT        NOT NULL,
    [State]                   TINYINT        NOT NULL,
    CONSTRAINT [PK_EventsDispatched] PRIMARY KEY CLUSTERED ([EventKey] ASC, [State] ASC),
    CONSTRAINT [CK_EventsDispatched] CHECK ([State]=(1))
);


GO
CREATE NONCLUSTERED INDEX [IX_EventsDispatched_AggregateKey]
    ON [dbo].[EventsDispatched]([AggregateKey] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_EventsDispatched_ApplicationKey_CorrelationKey]
    ON [dbo].[EventsDispatched]([ApplicationKey] ASC, [CorrelationKey] ASC);

