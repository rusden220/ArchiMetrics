﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UnusedEventRule.cs" company="Reimers.dk">
//   Copyright © Reimers.dk 2014
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the UnusedEventRule type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ArchiMetrics.CodeReview.Rules.Semantic
{
	using Microsoft.CodeAnalysis.CSharp;

	public class UnusedEventRule : UnusedCodeRule
	{
		public override string ID
		{
			get
			{
				return "AM0062";
			}
		}

		public override SyntaxKind EvaluatedKind
		{
			get { return SyntaxKind.EventDeclaration; }
		}
	}
}