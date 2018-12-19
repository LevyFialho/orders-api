using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using OrdersApi.Cqrs.Models;
using Xunit;

namespace OrdersApi.UnitTests.Cqrs.Models
{
    public class NewSequentialIdTests
    {
        [Fact]
        public void DuplicateIdTest()
        {
            var hashSet = new HashSet<string>();

            for (int i = 0; i < 1000000; i++)
            {
                hashSet.Add(IdentityGenerator.NewSequentialIdentity());
            }
        }



        [Fact]
        public async void DuplicateIdTestMultiTask()
        {
            var hashSet = new HashSet<string>();
            var tasks = new List<Task>();
            tasks.Add(Task.Run(() =>
            {
                for (int i = 0; i < 1000000; i++)
                {
                    var id = IdentityGenerator.NewSequentialIdentity();
                    lock (hashSet)
                    {
                        hashSet.Add(id);
                    }
                }
            }));
            tasks.Add(Task.Run(() =>
            {
                for (int i = 0; i < 1000000; i++)
                {
                    var id = IdentityGenerator.NewSequentialIdentity();
                    lock (hashSet)
                    {
                        hashSet.Add(id);
                    }
                }
            }));

            await Task.WhenAll(tasks);
        }

    }
}
