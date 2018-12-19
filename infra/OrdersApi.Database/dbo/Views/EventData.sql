

                CREATE VIEW [dbo].[EventData]
                WITH SCHEMABINDING
                AS
                SELECT [TargetVersion],[EventKey],[CorrelationKey],[AggregateKey],[ApplicationKey],
                [EventCommittedTimestamp], [ClassVersion], [Payload], [TimesSent], [State]
                 FROM [dbo].[EventsDispatched]
                UNION ALL
                SELECT [TargetVersion],[EventKey],[CorrelationKey],[AggregateKey],[ApplicationKey],
                [EventCommittedTimestamp], [ClassVersion], [Payload], [TimesSent], [State]
                 FROM [dbo].[EventsToDispatch]