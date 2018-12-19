using System.Diagnostics.CodeAnalysis; 

namespace OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage
{
    [ExcludeFromCodeCoverage]
    public class EntityFrameworkEventStorage: EntityFrameworkRepository<EventData>
    {
        public EntityFrameworkEventStorage(SqlEventStorageContext context) : base(context)
        {
        }
    }
}
