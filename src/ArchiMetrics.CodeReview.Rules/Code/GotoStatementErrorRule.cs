// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GotoStatementErrorRule.cs" company="Reimers.dk">
//   Copyright � Reimers.dk 2014
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the GotoStatementErrorRule type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ArchiMetrics.CodeReview.Rules.Code
{
    using Analysis.Common.CodeReview;
    using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;

	public class GotoStatementErrorRule : CodeEvaluationBase
	{
		public override string ID
		{
			get
			{
				return "AM0014";
			}
		}

		public override SyntaxKind EvaluatedKind
		{
			get
			{
				return SyntaxKind.GotoStatement;
			}
		}

		public override string Title
		{
			get
			{
				return "Goto Statement";
			}
		}

		public override string Suggestion
		{
			get
			{
				return "Refactor to use method calls.";
			}
		}

		public override CodeQuality Quality
		{
			get
			{
				return CodeQuality.Broken;
			}
		}

		public override QualityAttribute QualityAttribute
		{
			get
			{
				return QualityAttribute.Conformance;
			}
		}

		public override ImpactLevel ImpactLevel
		{
			get
			{
				return ImpactLevel.Member;
			}
		}

		protected override EvaluationResult EvaluateImpl(SyntaxNode node)
		{
			return new EvaluationResult
					   {
						   Snippet = node.ToFullString()
					   };
		}
	}
}
