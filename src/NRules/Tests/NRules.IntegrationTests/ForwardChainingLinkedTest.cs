using System.Linq;
using NRules.Fluent.Dsl;
using NRules.IntegrationTests.TestAssets;
using Xunit;

namespace NRules.IntegrationTests
{
    public class ForwardChainingLinkedTest : BaseRuleTestFixture
    {
        [Fact]
        public void Fire_OneMatchingFact_FiresFirstRuleAndChainsSecond()
        {
            //Arrange
            var fact1 = new FactType1 { TestProperty = "Valid Value 1", ChainProperty = "Valid Value 1" };
            Session.Insert(fact1);

            //Act
            Session.Fire();

            //Assert
            AssertFiredOnce<ForwardChainingFirstRule>();
            AssertFiredOnce<ForwardChainingSecondRule>();
        }

        [Fact]
        public void Fire_OneMatchingFactInsertedThenUpdated_FiresFirstRuleAndChainsSecondLinkedFactsUpdated()
        {
            //Arrange
            FactType2 matchedFact2 = null;
            FactType3 matchedFact3 = null;
            Session.Events.FactInsertedEvent += (sender, args) =>
            {
                if (args.Fact.Type == typeof(FactType2)) matchedFact2 = (FactType2) args.Fact.Value;
                if (args.Fact.Type == typeof(FactType3)) matchedFact3 = (FactType3) args.Fact.Value;
            };
            Session.Events.FactUpdatedEvent += (sender, args) =>
            {
                if (args.Fact.Type == typeof(FactType2)) matchedFact2 = (FactType2) args.Fact.Value;
                if (args.Fact.Type == typeof(FactType3)) matchedFact3 = (FactType3) args.Fact.Value;
            };

            var fact1 = new FactType1 { TestProperty = "Valid Value 1", ChainProperty = "Valid Value 1" };
            Session.Insert(fact1);

            //Act - I
            Session.Fire();

            //Assert - I
            AssertFiredOnce<ForwardChainingFirstRule>();
            AssertFiredOnce<ForwardChainingSecondRule>();
            Assert.Equal(1, matchedFact2.Count);
            Assert.Equal("Valid Value 1", matchedFact2.TestProperty);
            Assert.Equal("Valid Value 1", matchedFact3.TestProperty);

            //Act - II
            fact1.ChainProperty = "Valid Value 2";
            Session.Update(fact1);
            Session.Fire();

            //Assert - II
            AssertFiredTwice<ForwardChainingFirstRule>();
            AssertFiredTwice<ForwardChainingSecondRule>();
            Assert.Equal(2, matchedFact2.Count);
            Assert.Equal("Valid Value 2", matchedFact2.TestProperty);
            Assert.Equal("Valid Value 2", matchedFact3.TestProperty);
        }

        [Fact]
        public void Fire_OneMatchingFactInsertedThenRetracted_FiresFirstRuleAndChainsSecondLinkedFactsRetracted()
        {
            //Arrange
            var fact1 = new FactType1 { TestProperty = "Valid Value 1", ChainProperty = "Valid Value 1" };
            Session.Insert(fact1);

            //Act - I
            Session.Fire();

            //Assert - I
            AssertFiredOnce<ForwardChainingFirstRule>();
            AssertFiredOnce<ForwardChainingSecondRule>();
            Assert.Equal(1, Session.Query<FactType2>().Count());
            Assert.Equal(1, Session.Query<FactType3>().Count());

            //Act - II
            Session.Retract(fact1);

            //Assert - II
            Assert.Equal(0, Session.Query<FactType2>().Count());
            Assert.Equal(0, Session.Query<FactType3>().Count());
        }

        protected override void SetUpRules()
        {
            SetUpRule<ForwardChainingFirstRule>();
            SetUpRule<ForwardChainingSecondRule>();
        }

        public class FactType1
        {
            public string TestProperty { get; set; }
            public string ChainProperty { get; set; }
        }

        public class FactType2
        {
            public int Count { get; set; } = 1;
            public string TestProperty { get; set; }
        }

        public class FactType3
        {
            public string TestProperty { get; set; }
        }

        public class ForwardChainingFirstRule : Rule
        {
            public override void Define()
            {
                FactType1 fact1 = null;

                When()
                    .Match<FactType1>(() => fact1, f => f.TestProperty.StartsWith("Valid"));
                Then()
                    .Yield(ctx => new FactType2 {TestProperty = fact1.ChainProperty}, (ctx, fact2) => Update(fact1, fact2))
                    .Yield(ctx => new FactType3 {TestProperty = fact1.ChainProperty});
            }

            private static FactType2 Update(FactType1 fact1, FactType2 fact2)
            {
                fact2.TestProperty = fact1.ChainProperty;
                fact2.Count++;
                return fact2;
            }
        }

        public class ForwardChainingSecondRule : Rule
        {
            public override void Define()
            {
                FactType2 fact2 = null;
                FactType3 fact3 = null;

                When()
                    .Match<FactType2>(() => fact2, f => f.TestProperty.StartsWith("Valid"))
                    .Match<FactType3>(() => fact3, f => f.TestProperty.StartsWith("Valid"));
                Then()
                    .Do(ctx => ctx.NoOp());
            }
        }
    }
}