using System.Collections.Generic;
using System.Linq.Expressions;

namespace OrdersApi.Cqrs.Queries.Specifications
{
    /// <summary>
    /// Helper for rebinder parameters without use Invoke method in expressions 
    /// ( this methods is not supported in all linq query providers, 
    /// for example in Linq2Entities is not supported)
    /// </summary>
    public sealed class ParameterRebinder : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, ParameterExpression> map;

        /// <summary>
        /// Default construcotr
        /// </summary>
        /// <param name="map">Map specification</param>
        public ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
        {
            this.map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
        }

        /// <summary>
        /// Replate parameters in expression with a Map information
        /// </summary>
        /// <param name="map">Map information</param>
        /// <param name="exp">Expression to replace parameters</param>
        /// <returns>Expression with parameters replaced</returns>
        public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp)
        {
            return new ParameterRebinder(map).Visit(exp);
        }

        /// <summary>
        /// Visit pattern method
        /// </summary>
        /// <param name="node">A Parameter expression</param>
        /// <returns>New visited expression</returns>
        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (map.TryGetValue(node, out var replacement))
            {
                node = replacement;
            }

            return base.VisitParameter(node);
        }
    }
}
