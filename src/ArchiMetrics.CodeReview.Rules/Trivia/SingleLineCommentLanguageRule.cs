// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SingleLineCommentLanguageRule.cs" company="Reimers.dk">
//   Copyright � Reimers.dk 2014
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the SingleLineCommentLanguageRule type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ArchiMetrics.CodeReview.Rules.Trivia
{
    using Analysis.Common.CodeReview;
    using Microsoft.CodeAnalysis.CSharp;

	public class SingleLineCommentLanguageRule : CommentLanguageRuleBase
	{
		public SingleLineCommentLanguageRule(ISpellChecker spellChecker)
			: base(spellChecker)
		{
		}

		public override string ID
		{
			get
			{
				return "AM0068";
			}
		}

		public override string Title
		{
			get
			{
				return "Suspicious Language Single Line Comment";
			}
		}

		public override SyntaxKind EvaluatedKind
		{
			get { return SyntaxKind.SingleLineCommentTrivia; }
		}
	}
}
