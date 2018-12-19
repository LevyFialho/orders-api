using System.Collections;
using System.Collections.Generic;

namespace OrdersApi.Healthcheck.ComponentCheckers
{
    public class ComponentCollection<TComponent> : IEnumerable<TComponent>
    {
        public IEnumerable<TComponent> Items { get; set; } = new List<TComponent>();

        public IEnumerator<TComponent> GetEnumerator()
        {
            return this.Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Items.GetEnumerator();
        }
    }
}
