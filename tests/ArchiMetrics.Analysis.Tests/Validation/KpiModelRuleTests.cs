// --------------------------------------------------------------------------------------------------------------------
// <copyright file="KpiModelRuleTests.cs" company="Reimers.dk">
//   Copyright � Reimers.dk 2014
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the KpiModelRuleTests type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ArchiMetrics.Analysis.Tests.Validation
{
	using System.Linq;
	using System.Threading.Tasks;
	using ArchiMetrics.Analysis.Model;
	using ArchiMetrics.Analysis.Validation;
	using Common.CodeReview;
	using Xunit;

    public sealed class KpiModelRuleTests
	{
		private KpiModelRuleTests()
		{
		}

		public class GivenAKpiModelRule
		{
			[Fact]
			public async Task WhenTreeContainsComplexNodeThenIsNotPassed()
			{
				var rule = new KpiModelRule();
				var tree = new ModelNode(
					"tree", 
					NodeKind.Namespace, 
					CodeQuality.Good, 
					50, 
					30, 
					30, 
					new[] { new ModelNode("class", NodeKind.Class, CodeQuality.Good, 10, 90, 40) });

				var result = await rule.Validate(tree);

				Assert.Equal(1, result.Count());
			}

			[Fact]
			public async Task WhenTreeContainsLongNodeThenIsNotPassed()
			{
				var rule = new KpiModelRule();
				var tree = new ModelNode(
					"tree", 
					NodeKind.Namespace, 
					CodeQuality.Good, 
					50, 
					30, 
					30, 
					new[] { new ModelNode("class", NodeKind.Class, CodeQuality.Good, 100, 90, 10) });

				var result = await rule.Validate(tree);

				Assert.Equal(1, result.Count());
			}

			[Fact]
			public async Task WhenTreeContainsUnmaintainableNodeThenIsNotPassed()
			{
				var rule = new KpiModelRule();
				var tree = new ModelNode(
					"tree", 
					NodeKind.Namespace, 
					CodeQuality.Good, 
					50, 
					30, 
					30, 
					new[] { new ModelNode("class", NodeKind.Class, CodeQuality.Good, 10, 20, 1) });

				var result = await rule.Validate(tree);

				Assert.Equal(1, result.Count());
			}
		}
	}
}