using System;
using System.Collections.Generic;

namespace NRules.RuleModel.Builders
{
    /// <summary>
    /// Builder to compose forall element.
    /// </summary>
    public class ForAllBuilder : RuleElementBuilder, IBuilder<ForAllElement>
    {
        private PatternBuilder _basePatternBuilder;
        private readonly List<PatternBuilder> _patternBuilders = new List<PatternBuilder>();

        internal ForAllBuilder(SymbolTable scope)
            : base(scope)
        {
        }

        /// <summary>
        /// Creates a pattern builder that builds the base pattern of the forall element.
        /// </summary>
        /// <param name="type">Type of the element the pattern matches.</param>
        /// <returns>Pattern builder.</returns>
        public PatternBuilder BasePattern(Type type)
        {
            if (_basePatternBuilder != null)
            {
                throw new InvalidOperationException("FORALL element can only have a single source");
            }

            SymbolTable scope = Scope.New();
            Declaration declaration = scope.Declare(type, "$$base$$");

            _basePatternBuilder = new PatternBuilder(scope, declaration);
            return _basePatternBuilder;
        }
        
        /// <summary>
        /// Creates a pattern builder that builds a pattern of the forall element.
        /// </summary>
        /// <param name="type">Type of the element the pattern matches.</param>
        /// <returns>Pattern builder.</returns>
        public PatternBuilder Pattern(Type type)
        {
            SymbolTable scope = Scope.New();
            Declaration declaration = scope.Declare(type, null);

            var patternBuilder = new PatternBuilder(scope, declaration);
            _patternBuilders.Add(patternBuilder);
            return patternBuilder;
        }

        ForAllElement IBuilder<ForAllElement>.Build()
        {
            Validate();
            IBuilder<PatternElement> basePatternBuilder = _basePatternBuilder;
            PatternElement basePatternElement = basePatternBuilder.Build();

            var patternElements = new List<PatternElement>();
            foreach (IBuilder<PatternElement> patternBuilder in _patternBuilders)
            {
                patternElements.Add(patternBuilder.Build());
            }

            var existsElement = new ForAllElement(basePatternElement, patternElements);
            return existsElement;
        }

        private void Validate()
        {
            if (_basePatternBuilder == null)
            {
                throw new InvalidOperationException("FORALL element base pattern is not provided");
            }
            if (_patternBuilders.Count < 1)
            {
                throw new InvalidOperationException("At least one additional FORALL pattern must be specified");
            }
        }
    }
}