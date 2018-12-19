CREATE TABLE [dbo].[EventsToDispatch] (
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
    CONSTRAINT [PK_EventsToDispatch] PRIMARY KEY CLUSTERED ([EventKey] ASC, [State] ASC),
    CONSTRAINT [CK_EventsToDispatch] CHECK ([State]=(0))
);


GO
CREATE NONCLUSTERED INDEX [IX_EventsToDispatch_AggregateKey]
    ON [dbo].[EventsToDispatch]([AggregateKey] ASC);


GO
CREATE UNIQUE NONCLUSTERED INDEX [IX_EventsToDispatch_ApplicationKey_CorrelationKey]
    ON [dbo].[EventsToDispatch]([ApplicationKey] ASC, [CorrelationKey] ASC);

