namespace OrdersApi.Cqrs.Queries
{
    public enum SortDirection
    {
        Asc,
        Desc
    }

    public static class SortDirectionExtensions
    {
        public static SortDirection GetSortDirection(this string sortDirection)
        {
            if (string.IsNullOrWhiteSpace(sortDirection))
                return SortDirection.Desc;

            switch (sortDirection.ToUpper())
            {
                default: return SortDirection.Desc;
                case "ASC": return SortDirection.Asc;
                case "A": return SortDirection.Asc; 
            }
        }
    }
}
