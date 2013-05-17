﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HalsteadAnalyzer.cs" company="Roche">
//   Copyright © Roche 2012
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993] for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the HalsteadAnalyzer type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace ArchiMeter.CodeReview.Metrics
{
	using System.Collections.Generic;
	using System.Linq;
	using Common.Metrics;
	using Roslyn.Compilers.CSharp;
	using Roslyn.Compilers.Common;

	internal sealed class HalsteadAnalyzer : SyntaxWalker
	{
		private IHalsteadMetrics _metrics;

		public HalsteadAnalyzer()
			: base(SyntaxWalkerDepth.Node)
		{
		}

		public IHalsteadMetrics Calculate(MemberNode node)
		{
			BlockSyntax syntax = MemberBodySelector.FindBody(node);
			if (syntax != null)
			{
				Visit(syntax);
				return _metrics;
			}

			if (CalculateGenericPropertyMetrics(node))
			{
				return _metrics;
			}

			return new HalsteadMetrics(0, 0, 0, 0);
		}

		public override void VisitBlock(BlockSyntax node)
		{
			base.VisitBlock(node);
			IEnumerable<SyntaxToken> tokens = node.DescendantTokens().ToList();
			IDictionary<SyntaxKind, IList<string>> dictionary = ParseTokens(tokens, HalsteadOperands.All);
			IDictionary<SyntaxKind, IList<string>> dictionary2 = ParseTokens(tokens, HalsteadOperators.All);
			var metrics = new HalsteadMetrics(
				numOperands: dictionary.Values.Sum(x => x.Count), 
				numUniqueOperands: dictionary.Values.SelectMany(x => x).Distinct().Count(), 
				numOperators: dictionary2.Values.Sum(x => x.Count), 
				numUniqueOperators: dictionary2.Values.SelectMany(x => x).Distinct().Count());
			_metrics = metrics;
		}

		private static IDictionary<SyntaxKind, IList<string>> ParseTokens(IEnumerable<SyntaxToken> tokens, IEnumerable<SyntaxKind> filter)
		{
			IDictionary<SyntaxKind, IList<string>> dictionary = new Dictionary<SyntaxKind, IList<string>>();
			foreach (var token in tokens)
			{
				SyntaxKind kind = token.Kind;
				if (filter.Any(x => x == kind))
				{
					IList<string> list;
					string valueText = token.ValueText;
					if (!dictionary.TryGetValue(kind, out list))
					{
						dictionary[kind] = new List<string>();
						list = dictionary[kind];
					}

					list.Add(valueText);
				}
			}

			return dictionary;
		}

		private bool CalculateGenericPropertyMetrics(MemberNode node)
		{
			var syntaxNode = node.SyntaxNode as PropertyDeclarationSyntax;
			if (syntaxNode != null)
			{
				bool flag = syntaxNode.Modifiers.Any(x => x.ValueText == "static");
				if (MemberBodySelector.FindBody(node) == null)
				{
					switch (node.Kind)
					{
						case MemberKind.GetProperty:
							_metrics = flag ? HalsteadMetrics.GenericStaticGetPropertyMetrics : HalsteadMetrics.GenericInstanceGetPropertyMetrics;
							return true;

						case MemberKind.SetProperty:
							_metrics = flag ? HalsteadMetrics.GenericStaticSetPropertyMetrics : HalsteadMetrics.GenericInstanceSetPropertyMetrics;
							return true;
					}
				}
			}

			return false;
		}
	}
}