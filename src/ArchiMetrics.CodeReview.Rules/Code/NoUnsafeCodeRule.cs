// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NoUnsafeCodeRule.cs" company="Reimers.dk">
//   Copyright � Reimers.dk 2014
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the NoUnsafeCodeRule type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ArchiMetrics.CodeReview.Rules.Code
{
    using Analysis.Common.CodeReview;
    using Microsoft.CodeAnalysis;
	using Microsoft.CodeAnalysis.CSharp;

	public class NoUnsafeCodeRule : CodeEvaluationBase
	{
		public override string ID
		{
			get
			{
				return "AM0034";
			}
		}

		public override SyntaxKind EvaluatedKind
		{
			get
			{
				return SyntaxKind.UnsafeStatement;
			}
		}

		public override string Title
		{
			get
			{
				return "Unsafe Statement Detected";
			}
		}

		public override string Suggestion
		{
			get
			{
				return "Avoid unsafe code.";
			}
		}

		public override CodeQuality Quality
		{
			get
			{
				return CodeQuality.NeedsReEngineering;
			}
		}

		public override QualityAttribute QualityAttribute
		{
			get
			{
				return QualityAttribute.Conformance | QualityAttribute.Security;
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
			var snippet = node.ToFullString();
			return new EvaluationResult
				   {
					   Snippet = snippet
				   };
		}
	}
}