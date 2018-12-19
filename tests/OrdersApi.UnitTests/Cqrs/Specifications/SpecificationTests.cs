using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using OrdersApi.Cqrs.Models;
using OrdersApi.Cqrs.Queries.Specifications;
using FluentAssertions;
using Xunit;

namespace OrdersApi.UnitTests.Cqrs.Specifications
{ 
    public class SampleEntity
          : Entity
    {
        public string SampleProperty { get; set; }
    } 
    public class SpecificationTests
    { 
        [Fact] 
        public void TesteDeExceptionArgumentNullNaConstrucaoDeDirectSpecification()
        {
            //Arrange
            DirectSpecification<SampleEntity> adHocSpecification;
            Expression<Func<SampleEntity, bool>> spec = null;

            //Act
            Action act = () => adHocSpecification = new DirectSpecification<SampleEntity>(spec);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]

        public void TesteSpecificationAnd()
        {
            //Arrange
            DirectSpecification<SampleEntity> leftAdHocSpecification;
            DirectSpecification<SampleEntity> rightAdHocSpecification;

            var identifier = Guid.NewGuid().ToString();
            Expression<Func<SampleEntity, bool>> leftSpec = s => s.AggregateKey == identifier;
            Expression<Func<SampleEntity, bool>> rightSpec = s => s.SampleProperty.Length > 2;

            leftAdHocSpecification = new DirectSpecification<SampleEntity>(leftSpec);
            rightAdHocSpecification = new DirectSpecification<SampleEntity>(rightSpec);

            //Act
            AndSpecification<SampleEntity> composite = new AndSpecification<SampleEntity>(leftAdHocSpecification, rightAdHocSpecification);

            //Assert
            Assert.NotNull(composite.SatisfiedBy());
            Assert.Same(leftAdHocSpecification, composite.LeftSideSpecification);
            Assert.Same(rightAdHocSpecification, composite.RightSideSpecification);

            List<SampleEntity> list = new List<SampleEntity>();
            SampleEntity sampleA = new SampleEntity() { SampleProperty = "1" };
            sampleA.ChangeCurrentIdentity(identifier);

            SampleEntity sampleB = new SampleEntity() { SampleProperty = "the sample property" };
            sampleB.ChangeCurrentIdentity(identifier);

            list.AddRange(new SampleEntity[] { sampleA, sampleB });


            List<SampleEntity> result = list.AsQueryable().Where(composite.SatisfiedBy()).ToList();

            Assert.True(result.Count == 1);
        }

        [Fact]
         
        public void TestSpecificationOr()
        {
            //Arrange
            DirectSpecification<SampleEntity> leftAdHocSpecification;
            DirectSpecification<SampleEntity> rightAdHocSpecification;

            var identifier = Guid.NewGuid().ToString();
            Expression<Func<SampleEntity, bool>> leftSpec = s => s.AggregateKey == identifier;
            Expression<Func<SampleEntity, bool>> rightSpec = s => s.SampleProperty.Length > 2;

            leftAdHocSpecification = new DirectSpecification<SampleEntity>(leftSpec);
            rightAdHocSpecification = new DirectSpecification<SampleEntity>(rightSpec);

            //Act
            OrSpecification<SampleEntity> composite = new OrSpecification<SampleEntity>(leftAdHocSpecification, rightAdHocSpecification);

            //Assert
            Assert.NotNull(composite.SatisfiedBy());
            Assert.Same(leftAdHocSpecification, composite.LeftSideSpecification);
            Assert.Same(rightAdHocSpecification, composite.RightSideSpecification);

            List<SampleEntity> list = new List<SampleEntity>();

            SampleEntity sampleA = new SampleEntity() { SampleProperty = "1" };
            sampleA.ChangeCurrentIdentity(identifier);

            SampleEntity sampleB = new SampleEntity() { SampleProperty = "the sample property" };
            sampleB.GenerateNewIdentity();

            list.AddRange(new SampleEntity[] { sampleA, sampleB });


            List<SampleEntity> result = list.AsQueryable().Where(composite.SatisfiedBy()).ToList();

            Assert.True(result.Count() == 2);
        }

        [Fact]
          
        public void TesteSpecificationAndComArgummentNull()
        {
            //Arrange
            DirectSpecification<SampleEntity> leftAdHocSpecification;
            DirectSpecification<SampleEntity> rightAdHocSpecification;
            AndSpecification<SampleEntity> composite;
            Expression<Func<SampleEntity, bool>> leftSpec = s => s.AggregateKey == Guid.NewGuid().ToString();
            Expression<Func<SampleEntity, bool>> rightSpec = s => s.SampleProperty.Length > 2;

            leftAdHocSpecification = new DirectSpecification<SampleEntity>(leftSpec);
            rightAdHocSpecification = new DirectSpecification<SampleEntity>(rightSpec);

            //Act
            Action act = () => composite = new AndSpecification<SampleEntity>(null, rightAdHocSpecification);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact] 
        public void TesteSpecificationAndComArgummentNullRight()
        {
            //Arrange
            DirectSpecification<SampleEntity> leftAdHocSpecification;
            DirectSpecification<SampleEntity> rightAdHocSpecification;

            Expression<Func<SampleEntity, bool>> rightSpec = s => s.AggregateKey == Guid.NewGuid().ToString();
            Expression<Func<SampleEntity, bool>> leftSpec = s => s.SampleProperty.Length > 2;

            leftAdHocSpecification = new DirectSpecification<SampleEntity>(leftSpec);
            rightAdHocSpecification = new DirectSpecification<SampleEntity>(rightSpec);

            //Act
            AndSpecification<SampleEntity> composite;
            Action act = () => composite = new AndSpecification<SampleEntity>(leftAdHocSpecification, null);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void TesteSpecificationOrComArgummentNullLeft()
        {
            //Arrange
            DirectSpecification<SampleEntity> leftAdHocSpecification;
            DirectSpecification<SampleEntity> rightAdHocSpecification;

            Expression<Func<SampleEntity, bool>> leftSpec = s => s.AggregateKey == Guid.NewGuid().ToString();
            Expression<Func<SampleEntity, bool>> rightSpec = s => s.SampleProperty.Length > 2;

            leftAdHocSpecification = new DirectSpecification<SampleEntity>(leftSpec);
            rightAdHocSpecification = new DirectSpecification<SampleEntity>(rightSpec);

            //Act
            OrSpecification<SampleEntity> composite;
            Action act = () => composite = new OrSpecification<SampleEntity>(null, rightAdHocSpecification);
            act.Should().Throw<ArgumentNullException>();

        }

        [Fact]
          
        public void TesteSpecificationOrComArgummentNullRight()
        {
            //Arrange
            DirectSpecification<SampleEntity> leftAdHocSpecification;
            DirectSpecification<SampleEntity> rightAdHocSpecification;

            Expression<Func<SampleEntity, bool>> rightSpec = s => s.AggregateKey == Guid.NewGuid().ToString();
            Expression<Func<SampleEntity, bool>> leftSpec = s => s.SampleProperty.Length > 2;

            leftAdHocSpecification = new DirectSpecification<SampleEntity>(leftSpec);
            rightAdHocSpecification = new DirectSpecification<SampleEntity>(rightSpec);
            OrSpecification<SampleEntity> composite;
            //Act
            Action act = () => composite  = new OrSpecification<SampleEntity>(leftAdHocSpecification, null);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
         
        public void TesteUsoSpecificationAnd()
        {
            //Arrange
            DirectSpecification<SampleEntity> leftAdHocSpecification;
            DirectSpecification<SampleEntity> rightAdHocSpecification;

            var identifier = Guid.NewGuid().ToString();
            Expression<Func<SampleEntity, bool>> leftSpec = s => s.AggregateKey == identifier;
            Expression<Func<SampleEntity, bool>> rightSpec = s => s.SampleProperty.Length > 2;

            //Act
            leftAdHocSpecification = new DirectSpecification<SampleEntity>(leftSpec);
            rightAdHocSpecification = new DirectSpecification<SampleEntity>(rightSpec);

            ISpecification<SampleEntity> andSpec = leftAdHocSpecification && rightAdHocSpecification;

            List<SampleEntity> list = new List<SampleEntity>();

            SampleEntity sampleA = new SampleEntity() { SampleProperty = "1" };
            sampleA.ChangeCurrentIdentity(identifier);

            SampleEntity sampleB = new SampleEntity() { SampleProperty = "the sample property" };
            sampleB.GenerateNewIdentity();

            SampleEntity sampleC = new SampleEntity() { SampleProperty = "the sample property" };
            sampleC.ChangeCurrentIdentity(identifier);

            list.AddRange(new SampleEntity[] { sampleA, sampleB, sampleC });

            List<SampleEntity> result = list.AsQueryable().Where(andSpec.SatisfiedBy()).ToList();

            Assert.True(result.Count == 1);
        }

        [Fact]
         
        public void TesteUsoSpecificationOr()
        {
            //Arrange
            DirectSpecification<SampleEntity> leftAdHocSpecification;
            DirectSpecification<SampleEntity> rightAdHocSpecification;

            var identifier = Guid.NewGuid().ToString();
            Expression<Func<SampleEntity, bool>> leftSpec = s => s.AggregateKey == identifier;
            Expression<Func<SampleEntity, bool>> rightSpec = s => s.SampleProperty.Length > 2;

            //Act
            leftAdHocSpecification = new DirectSpecification<SampleEntity>(leftSpec);
            rightAdHocSpecification = new DirectSpecification<SampleEntity>(rightSpec);

            Specification<SampleEntity> orSpec = leftAdHocSpecification || rightAdHocSpecification;

            //Assert
            List<SampleEntity> list = new List<SampleEntity>();
            SampleEntity sampleA = new SampleEntity() { SampleProperty = "1" };
            sampleA.ChangeCurrentIdentity(identifier);

            SampleEntity sampleB = new SampleEntity() { SampleProperty = "the sample property" };
            sampleB.GenerateNewIdentity();

            list.AddRange(new SampleEntity[] { sampleA, sampleB });

            List<SampleEntity> result = list.AsQueryable().Where(orSpec.SatisfiedBy()).ToList();

            Assert.True(result.Count() == 2);
        }

        [Fact]
         
        public void TesteChecarOperadoresNot()
        {
            //Arrange
            Expression<Func<SampleEntity, bool>> specificationCriteria = t => t.AggregateKey == Guid.NewGuid().ToString();

            //Act
            Specification<SampleEntity> spec = new DirectSpecification<SampleEntity>(specificationCriteria);
            Specification<SampleEntity> notSpec = !spec;
            ISpecification<SampleEntity> resultAnd = notSpec && spec;
            ISpecification<SampleEntity> resultOr = notSpec || spec;

            //Assert
            Assert.NotNull(notSpec);
            Assert.NotNull(resultAnd);
            Assert.NotNull(resultOr);
        }

        [Fact]
        public void TesteSpecificationNotComArgummentNull()
        {
            //Arrange
            NotSpecification<SampleEntity> notSpec;

            //Act
            Action act = () => notSpec = new NotSpecification<SampleEntity>((ISpecification<SampleEntity>)null);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
         
        public void TesteSpecificationTrue()
        {
            //Arrange
            ISpecification<SampleEntity> trueSpec = new TrueSpecification<SampleEntity>();
            bool expected = true;
            bool actual = trueSpec.SatisfiedBy().Compile()(new SampleEntity());
            //Assert
            Assert.NotNull(trueSpec);
            Assert.Equal(expected, actual);
        }
    }
}
