// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ModelValidator.cs" company="Reimers.dk">
//   Copyright � Matthias Friedrich, Reimers.dk 2014
//   This source is subject to the MIT License.
//   Please see https://opensource.org/licenses/MIT for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the ModelValidator type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ArchiMetrics.Analysis.Validation
{
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using ArchiMetrics.Analysis.Model;
	using Common;
	using Common.CodeReview;
	using Common.Structure;

    internal class ModelValidator : IModelValidator
	{
		private readonly IVertexRepository _repository;
		private readonly ISyntaxTransformer _syntaxTransformer;

		public ModelValidator(
			ISyntaxTransformer syntaxTransformer, 
			IVertexRepository repository)
		{
			_syntaxTransformer = syntaxTransformer;
			_repository = repository;
		}

		public async Task<IEnumerable<IValidationResult>> Validate(string solutionPath, IEnumerable<IModelRule> rules, IEnumerable<TransformRule> transformRules, CancellationToken cancellationToken)
		{
			var model = await _repository.GetVertices(solutionPath, cancellationToken).ConfigureAwait(false);
			var transformed = await _syntaxTransformer.Transform(model, transformRules, cancellationToken).ConfigureAwait(false);
			var modelTree = new ModelNode("All", NodeKind.Solution, CodeQuality.Good, 0, 0, 0, transformed.ToList());
			var tasks = rules.Select(r => r.Validate(modelTree));
			var results = await Task.WhenAll(tasks).ConfigureAwait(false);

			return results.SelectMany(x => x).AsArray();
		}
	}
}
