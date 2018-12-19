using System;
using System.Collections.Generic;
using System.Text;
using OrdersApi.Infrastructure.StorageProviders.SqlServer.EventStorage;
using Xunit;

namespace OrdersApi.UnitTests.Infrastructure.SqlEventStore
{
    public class StringCompressorTests
    {
        [Fact]
        public void CompressorTest()
        {
            var value = Guid.NewGuid().ToString();
            var compressedValue = StringCompressor.CompressString(value);
            var decompressedValue = StringCompressor.DecompressString(compressedValue);
            Assert.Equal(value, decompressedValue);
        }
    }
}
