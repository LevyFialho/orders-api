using System;
using OrdersApi.Cqrs.Models;
using Moq;
using Xunit;

namespace OrdersApi.UnitTests.Cqrs.Models
{
    /// <summary>
    /// Entity tests
    /// </summary> 
    public class EntityTests
    {
        public class TestEntity : Entity
        {
            
        }

        [Fact]
        public void IsTransientTest()
        { 
            var e = new TestEntity(); 
            Assert.True(e.IsTransient());
            e.GenerateNewIdentity();
            Assert.False(e.IsTransient());
        }

        [Fact]
        public void GetHashCodeTest()
        {
            var e = new TestEntity();
            e.GenerateNewIdentity();
            var hash = e.GetHashCode();
            Assert.Equal(e.AggregateKey.GetHashCode() ^ 31, hash);
        }


        [Fact]
        public void ToStringTest()
        {
            var e = new TestEntity();
            e.GenerateNewIdentity();
            var hash = e.ToString();
            Assert.Equal(e.GetType().Name + " [Id=" + e.AggregateKey + "]", hash);
        }

        [Fact]
        public void GenerateNewIdentityTest()
        {
            Mock<Entity> e = new Mock<Entity> { CallBase = true };
            e.Setup(x => x.IsTransient()).Returns(false);
            e.Object.GenerateNewIdentity();
            Assert.Null(e.Object.AggregateKey); 
            e.Setup(x => x.IsTransient()).Returns(true);
            e.Object.GenerateNewIdentity();
            Assert.False(string.IsNullOrWhiteSpace(e.Object.AggregateKey)); 
        }
         

        [Fact]
        public void ChangeCurrentIdentityTest()
        {

            Mock<Entity> e = new Mock<Entity> { CallBase = true };
            e.Object.GenerateNewIdentity();
            var oldId = e.Object.AggregateKey;
            var id = "";
            e.Object.ChangeCurrentIdentity(id);
            Assert.Equal(oldId, e.Object.AggregateKey);
            id = Guid.NewGuid().ToString();
            e.Object.ChangeCurrentIdentity(id);
            Assert.Equal(id, e.Object.AggregateKey);
        }

        [Fact]
        public void TwoEntitiesWithSameIdEqualsTest()
        {
            //Arrange
            var id = Guid.NewGuid().ToString();

            var entityLeft = new Mock<Entity>() { CallBase = true };
            var entityRight = new Mock<Entity>() { CallBase = true }; ;

            entityLeft.Object.ChangeCurrentIdentity(id);
            entityRight.Object.ChangeCurrentIdentity(id);

            //Act
            bool resultOnEquals = entityLeft.Object.Equals(entityRight.Object);
            bool resultOnOperator = entityLeft.Object == entityRight.Object;

            //Assert
            Assert.True(resultOnEquals);
            Assert.True(resultOnOperator);

        }

        [Fact]
        public void TwoEntitiesWithDifferentIdEqualsTest()
        {
            //Arrange

            var entityLeft = new Mock<Entity>() { CallBase = true }; ;
            var entityRight = new Mock<Entity>() { CallBase = true }; ;

            entityLeft.Object.GenerateNewIdentity();
            entityRight.Object.GenerateNewIdentity();


            //Act
            bool resultOnEquals = entityLeft.Equals(entityRight);
            bool resultOnOperator = entityLeft == entityRight;

            //Assert
            Assert.False(resultOnEquals);
            Assert.False(resultOnOperator);

        }

        [Fact]
        public void CompareSameReferenceTest()
        {
            //Arrange
            var entityLeft = new Mock<Entity>() { CallBase = true }; ;
            Mock<Entity> entityRight = entityLeft;


            //Act
            Assert.True(entityLeft.Object.Equals(entityRight.Object));
            Assert.True(entityLeft.Object == entityRight.Object);
        }

    }
}
