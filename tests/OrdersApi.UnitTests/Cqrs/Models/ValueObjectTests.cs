using OrdersApi.Cqrs.Models;
using Xunit;
#pragma warning disable CS1718

namespace OrdersApi.UnitTests.Cqrs.Models
{
    public class ValueObjectTests
    {
        public class Address
            : ValueObject<Address>
        {
            public string StreetLine1 { get; private set; }
            public string StreetLine2 { get; private set; }
            public string City { get; private set; }
            public string ZipCode { get; private set; }

            public Address(string streetLine1, string streetLine2, string city, string zipCode)
            {
                this.StreetLine1 = streetLine1;
                this.StreetLine2 = streetLine2;
                this.City = city;
                this.ZipCode = zipCode;
            }
        }

        public class SelfReference
            : ValueObject<SelfReference>
        {
            public SelfReference()
            {
            }
            public SelfReference(SelfReference value)
            {
                Value = value;
            }
            public SelfReference Value { get; set; }
        }

        [Fact]
        public void IdenticalDataEqualsTrueTest()
        {
            //Arrange
            Address address1 = new Address("streetLine1", "streetLine2", "city", "zipcode");
            Address address2 = new Address("streetLine1", "streetLine2", "city", "zipcode");

            //Act
            bool resultEquals = address1.Equals(address2);
            bool resultEqualsSimetric = address2.Equals(address1);
            bool resultEqualsOnThis = address1.Equals(address1);

            //Assert
            Assert.True(resultEquals);
            Assert.True(resultEqualsSimetric);
            Assert.True(resultEqualsOnThis);
        }

        [Fact]
        public void IdenticalDataEqualOperatorTrueTest()
        {
            //Arraneg
            Address address1 = new Address("streetLine1", "streetLine2", "city", "zipcode");
            Address address2 = new Address("streetLine1", "streetLine2", "city", "zipcode");

            //Act
            bool resultEquals = (address1 == address2);
            bool resultEqualsSimetric = (address2 == address1);
            bool resultEqualsOnThis = (address1 == address1);

            //Assert
            Assert.True(resultEquals);
            Assert.True(resultEqualsSimetric);
            Assert.True(resultEqualsOnThis);
        }

        [Fact]
        public void IdenticalDataIsNotEqualOperatorFalseTest()
        {
            //Arraneg
            Address address1 = new Address("streetLine1", "streetLine2", "city", "zipcode");
            Address address2 = new Address("streetLine1", "streetLine2", "city", "zipcode");

            //Act
            bool resultEquals = (address1 != address2);
            bool resultEqualsSimetric = (address2 != address1);
            bool resultEqualsOnThis = (address1 != address1);

            //Assert
            Assert.False(resultEquals);
            Assert.False(resultEqualsSimetric);
            Assert.False(resultEqualsOnThis);
        }

        [Fact]
        public void DiferentDataEqualsFalseTest()
        {
            //Arrange
            Address address1 = new Address("streetLine1", "streetLine2", "city", "zipcode");
            Address address2 = new Address("streetLine2", "streetLine1", "city", "zipcode");

            //Act
            bool result = address1.Equals(address2);
            bool resultSimetric = address2.Equals(address1);

            //Assert
            Assert.False(result);
            Assert.False(resultSimetric);
        }

        [Fact]
        public void DiferentDataIsNotEqualOperatorTrueTest()
        {
            //Arrange
            Address address1 = new Address("streetLine1", "streetLine2", "city", "zipcode");
            Address address2 = new Address("streetLine2", "streetLine1", "city", "zipcode");

            //Act
            bool result = (address1 != address2);
            bool resultSimetric = (address2 != address1);

            //Assert
            Assert.True(result);
            Assert.True(resultSimetric);
        }

        [Fact]
        public void DiferentDataEqualOperatorFalseTest()
        {
            //Arrange
            Address address1 = new Address("streetLine1", "streetLine2", "city", "zipcode");
            Address address2 = new Address("streetLine2", "streetLine1", "city", "zipcode");

            //Act
            bool result = (address1 == address2);
            bool resultSimetric = (address2 == address1);

            //Assert
            Assert.False(result);
            Assert.False(resultSimetric);
        }

        [Fact]
        public void SameDataInDiferentPropertiesIsEqualsFalseTest()
        {
            //Arrange
            Address address1 = new Address("streetLine1", "streetLine2", null, null);
            Address address2 = new Address("streetLine2", "streetLine1", null, null);

            //Act
            bool result = address1.Equals(address2);


            //Assert
            Assert.False(result);
        }

        [Fact]
        public void SameDataInDiferentPropertiesEqualOperatorFalseTest()
        {
            //Arrange
            Address address1 = new Address("streetLine1", "streetLine2", null, null);
            Address address2 = new Address("streetLine2", "streetLine1", null, null);

            //Act
            bool result = (address1 == address2);


            //Assert
            Assert.False(result);
        }

        [Fact]
        public void DiferentDataInDiferentPropertiesProduceDiferentHashCodeTest()
        {
            //Arrange
            Address address1 = new Address("streetLine1", "streetLine2", null, null);
            Address address2 = new Address("streetLine2", "streetLine1", null, null);

            //Act
            int address1HashCode = address1.GetHashCode();
            int address2HashCode = address2.GetHashCode();


            //Assert
            Assert.NotEqual(address1HashCode, address2HashCode);
        }

        [Fact]
        public void SameDataInDiferentPropertiesProduceDiferentHashCodeTest()
        {
            //Arrange
            Address address1 = new Address("streetLine1", null, null, "streetLine1");
            Address address2 = new Address(null, "streetLine1", "streetLine1", null);

            //Act
            int address1HashCode = address1.GetHashCode();
            int address2HashCode = address2.GetHashCode();


            //Assert
            Assert.NotEqual(address1HashCode, address2HashCode);
        }

        [Fact]
        public void SameReferenceEqualsTrueTest()
        {
            //Arrange
            Address address1 = new Address("streetLine1", null, null, "streetLine1");
            Address address2 = address1;


            //Act
            Assert.True(address1.Equals(address2));
            Assert.True(address1 == address2);
        }

        [Fact]
        public void SameDataSameHashCodeTest()
        {
            //Arrange
            Address address1 = new Address("streetLine1", "streetLine2", null, null);
            Address address2 = new Address("streetLine1", "streetLine2", null, null);

            //Act
            int address1HashCode = address1.GetHashCode();
            int address2HashCode = address2.GetHashCode();


            //Assert
            Assert.Equal(address1HashCode, address2HashCode);
        }

        [Fact]
        public void SelfReferenceNotProduceInfiniteLoop()
        {
            //Arrange
            SelfReference aReference = new SelfReference();
            SelfReference bReference = new SelfReference();

            //Act
            aReference.Value = bReference;
            bReference.Value = aReference;

            //Assert

            Assert.NotEqual(aReference, bReference);
        }
    }
}
