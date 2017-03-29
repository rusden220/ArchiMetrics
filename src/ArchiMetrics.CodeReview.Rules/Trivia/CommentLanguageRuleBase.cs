// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CommentLanguageRuleBase.cs" company="Reimers.dk">
//   Copyright � Reimers.dk 2014
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the CommentLanguageRuleBase type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ArchiMetrics.CodeReview.Rules.Trivia
{
	using System.Linq;
	using System.Text.RegularExpressions;
	using Analysis.Common;
	using Analysis.Common.CodeReview;
	using Microsoft.CodeAnalysis;

	public abstract class CommentLanguageRuleBase : TriviaEvaluationBase
	{
		private static readonly Regex StrippedRegex = new Regex(@"[""'*���()!%\[\]{}/]+", RegexOptions.Compiled);
		private static readonly Regex LineDashRegex = new Regex(@"-{3,}", RegexOptions.Compiled);
		private static readonly Regex NumberRegex = new Regex("[1-9]+", RegexOptions.Compiled);
		private static readonly Regex XmlRegex = new Regex("<.+?>", RegexOptions.Compiled);
		private readonly ISpellChecker _spellChecker;

		protected CommentLanguageRuleBase(ISpellChecker spellChecker)
		{
			_spellChecker = spellChecker;
		}

		public override string Suggestion
		{
			get
			{
				return "Check spelling of comment.";
			}
		}

		public override CodeQuality Quality
		{
			get
			{
				return CodeQuality.NeedsReview;
			}
		}

		public override QualityAttribute QualityAttribute
		{
			get
			{
				return QualityAttribute.Maintainability | QualityAttribute.Conformance;
			}
		}

		public override ImpactLevel ImpactLevel
		{
			get
			{
				return ImpactLevel.Member;
			}
		}

		protected override EvaluationResult EvaluateImpl(SyntaxTrivia node)
		{
			var trimmed = StrippedRegex.Replace(node.ToFullString(), string.Empty).Trim();
			var commentWords = RemoveLineDashes(RemoveXml(trimmed))
				.Split(' ')
				.Select(RemoveXml)
				.Select(s => s.TrimEnd('.', ',', '_'))
				.Where(IsNotNumber)
				.AsArray();
			var errorCount = commentWords.Aggregate(0, (i, s) => i + (_spellChecker.Spell(s) ? 0 : 1));
			if (errorCount >= 0.50 * commentWords.Length)
			{
				return new EvaluationResult
						   {
							   Snippet = node.ToFullString()
						   };
			}

			return null;
		}

		private bool IsNotNumber(string input)
		{
			return !NumberRegex.IsMatch(input);
		}

		private string RemoveLineDashes(string input)
		{
			return LineDashRegex.Replace(input, string.Empty);
		}

		private string RemoveXml(string input)
		{
			return XmlRegex.Replace(input, string.Empty);
		}
	}
}
