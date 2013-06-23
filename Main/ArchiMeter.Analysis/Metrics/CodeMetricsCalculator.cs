// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CodeMetricsCalculator.cs" company="Roche">
//   Copyright � Roche 2012
//   This source is subject to the Microsoft Public License (Ms-PL).
//   Please see http://go.microsoft.com/fwlink/?LinkID=131993] for details.
//   All other rights reserved.
// </copyright>
// <summary>
//   Defines the CodeMetricsCalculator type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace ArchiMeter.Analysis.Metrics
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;
	using System.Xml.Linq;
	using Common.Metrics;
	using Roslyn.Compilers.Common;
	using Roslyn.Compilers.CSharp;
	using Roslyn.Services;

	public class CodeMetricsCalculator : ICodeMetricsCalculator
	{
		private static readonly List<Regex> Patterns = new List<Regex>
													   {
														   new Regex(@".*\.g\.cs$", RegexOptions.Compiled), 
														   new Regex(@".*\.g\.i\.cs$", RegexOptions.Compiled), 
															   
														   // new Regex(@".*\.xaml\.cs$", RegexOptions.Compiled), 
														   new Regex(@".*\.designer\.cs$", RegexOptions.Compiled)
													   };

		private readonly XamlConverter _converter;
		private readonly SyntaxCollector _syntaxCollector = new SyntaxCollector();

		public CodeMetricsCalculator()
		{
			_converter = new XamlConverter();
			IgnoreGeneratedCode = true;
		}

		public bool IgnoreGeneratedCode { get; set; }

		public virtual Task<IEnumerable<NamespaceMetric>> Calculate(IProject project)
		{
			return Task.Factory
				.StartNew(() =>
							  {
								  var calcProject = project.HasDocuments
														? project
														: GetDocuments(project);
								  var compilation = calcProject.GetCompilation();
								  var namespaceDeclarations = GetNamespaceDeclarations(calcProject, IgnoreGeneratedCode);
								  return CalculateNamespaceMetrics(namespaceDeclarations.Select(d => d.Value), compilation);
							  });
		}

		public Task<IEnumerable<NamespaceMetric>> Calculate(IEnumerable<SyntaxTree> syntaxTrees)
		{
			return Task.Factory.StartNew(() =>
				{
					var trees = syntaxTrees.ToArray();
					var commonCompilation = Compilation.Create("x", syntaxTrees: trees);
					var declarations = _syntaxCollector.GetDeclarations(trees);
					var statementMembers = declarations.Statements.Select(s => Syntax.MethodDeclaration(
						Syntax.PredefinedType(
						Syntax.Token(SyntaxKind.ObjectKeyword)),
						Guid.NewGuid().ToString("N"))
						.WithBody(Syntax.Block(s)));
					var members = declarations.MemberDeclarations.Concat(statementMembers).ToArray();
					var anonClass = members.Any()
						? new[]
						  {
							  Syntax.ClassDeclaration(
								  "UnnamedClass")
								  .WithModifiers(Syntax.Token(SyntaxKind.PublicKeyword))
								  .WithMembers(Syntax.List(members))
						  }
						: new TypeDeclarationSyntax[0];
					var array = declarations.TypeDeclarations
						.Concat(anonClass)
						.Cast<MemberDeclarationSyntax>()
						.ToArray();
					var anonNs = array.Any()
						? new[]
						  {
							  Syntax.NamespaceDeclaration(Syntax.ParseName("Unnamed"))
								  .WithMembers(Syntax.List(array))
						  }
						: new NamespaceDeclarationSyntax[0];
					var namespaceDeclarations = declarations
						.NamespaceDeclarations
						.Concat(anonNs)
						.Select(x => new NamespaceDeclarationSyntaxInfo
									 {
										 Name = x.GetName(x),
										 Syntax = x
									 })
						.GroupBy(x => x.Name)
						.Select(g => new NamespaceDeclaration
									 {
										 Name = g.Key,
										 SyntaxNodes = g.ToArray()
									 })
						.ToArray();

					var namespaceMetrics = CalculateNamespaceMetrics(namespaceDeclarations, commonCompilation);
					return namespaceMetrics;
				});
		}

		private static IEnumerable<NamespaceMetric> CalculateNamespaceMetrics(IEnumerable<NamespaceDeclaration> namespaceDeclarations, CommonCompilation compilation)
		{
			var metrics = namespaceDeclarations.Select(declaration => declaration)
											   .Select(
												   arg =>
												   {
													   var tuple = CalculateTypeMetrics(compilation, arg);
													   return new
															  {
																  NamespaceDeclaration = arg,
																  Compilation = tuple.Item1,
																  Metrics = tuple.Item2
															  };
												   })
											   .Select(b => CalculateNamespaceMetrics(b.Compilation, b.NamespaceDeclaration, b.Metrics))
											   .Select(t => t.Item2);
			return metrics;
		}

		private static Tuple<CommonCompilation, IEnumerable<MemberMetric>> CalculateMemberMetrics(CommonCompilation compilation, TypeDeclaration typeNodes)
		{
			var comp = compilation;
			var metrics = typeNodes.SyntaxNodes
				.SelectMany(info =>
							{
								var tuple = VerifyCompilation(comp, info);
								var semanticModel = tuple.Item2;
								comp = tuple.Item1;
								var calculator = new MemberMetricsCalculator(semanticModel);

								return calculator.Calculate(info);
							});
			return new Tuple<CommonCompilation, IEnumerable<MemberMetric>>(comp, metrics.ToArray());
		}

		private static Tuple<CommonCompilation, NamespaceMetric> CalculateNamespaceMetrics(CommonCompilation compilation, NamespaceDeclaration namespaceNodes, IEnumerable<TypeMetric> typeMetrics)
		{
			var namespaceNode = namespaceNodes.SyntaxNodes.FirstOrDefault();
			if (namespaceNode == null)
			{
				return null;
			}

			var tuple = VerifyCompilation(compilation, namespaceNode);
			compilation = tuple.Item1;
			var semanticModel = compilation.GetSemanticModel(namespaceNode.Syntax.SyntaxTree);
			var calculator = new NamespaceMetricsCalculator(semanticModel);
			return new Tuple<CommonCompilation, NamespaceMetric>(compilation, calculator.CalculateFrom(namespaceNode, typeMetrics));
		}

		private static Tuple<CommonCompilation, IEnumerable<TypeMetric>> CalculateTypeMetrics(CommonCompilation compilation, NamespaceDeclaration namespaceNodes)
		{
			var comp = compilation;
			var typeMetrics = GetTypeDeclarations(namespaceNodes)
				.Select(pair => pair.Value)
				.Select(typeNodes =>
						{
							var tuple = CalculateMemberMetrics(comp, typeNodes);
							var metrics = tuple.Item2;
							comp = tuple.Item1;
							return new
								   {
									   comp,
									   typeNodes,
									   memberMetrics = metrics
								   };
						})
				.Select(@t =>
						{
							var tuple = CalculateTypeMetrics(t.comp, t.typeNodes, t.memberMetrics);
							comp = tuple.Item1;
							return tuple.Item2;
						})
				.ToArray();

			return new Tuple<CommonCompilation, IEnumerable<TypeMetric>>(comp, typeMetrics);
		}

		private static Tuple<CommonCompilation, TypeMetric> CalculateTypeMetrics(CommonCompilation compilation, TypeDeclaration typeNodes, IEnumerable<MemberMetric> memberMetrics)
		{
			if (typeNodes.SyntaxNodes.Any())
			{
				var tuple = VerifyCompilation(compilation, typeNodes.SyntaxNodes.First());
				var semanticModel = tuple.Item2;
				compilation = tuple.Item1;
				var typeNode = tuple.Item3;
				var calculator = new TypeMetricsCalculator(semanticModel);
				return new Tuple<CommonCompilation, TypeMetric>(
					compilation,
					calculator.CalculateFrom(typeNode, memberMetrics));
			}

			return null;
		}

		private static Tuple<CommonCompilation, ISemanticModel, TypeDeclarationSyntaxInfo> VerifyCompilation(CommonCompilation compilation, TypeDeclarationSyntaxInfo typeNode)
		{
			ISemanticModel semanticModel;
			if (typeNode.Syntax.SyntaxTree == null)
			{
				var cu = SyntaxTree.Create(
					Syntax
					.CompilationUnit()
					.WithMembers(Syntax.List((MemberDeclarationSyntax)typeNode.Syntax)));
				typeNode.Syntax = cu.GetRoot()
					.ChildNodes()
					.First();
				var newCompilation = compilation.AddSyntaxTrees(cu);
				semanticModel = newCompilation.GetSemanticModel(cu);
				return new Tuple<CommonCompilation, ISemanticModel, TypeDeclarationSyntaxInfo>(
					newCompilation, semanticModel, typeNode);
			}
			if (!compilation.ContainsSyntaxTree(typeNode.Syntax.SyntaxTree))
			{
				compilation = compilation.AddSyntaxTrees(typeNode.Syntax.SyntaxTree);
			}
			semanticModel = compilation.GetSemanticModel(typeNode.Syntax.SyntaxTree);
			return new Tuple<CommonCompilation, ISemanticModel, TypeDeclarationSyntaxInfo>(
				compilation,
				semanticModel,
				typeNode);
		}

		private static Tuple<CommonCompilation, ISemanticModel, NamespaceDeclarationSyntaxInfo> VerifyCompilation(CommonCompilation compilation, NamespaceDeclarationSyntaxInfo namespaceNode)
		{
			ISemanticModel semanticModel;
			if (namespaceNode.Syntax.SyntaxTree == null)
			{
				var cu = SyntaxTree.Create(
					Syntax
					.CompilationUnit()
					.WithMembers(Syntax.List((MemberDeclarationSyntax)namespaceNode.Syntax)));
				namespaceNode.Syntax = cu.GetRoot()
					.ChildNodes()
					.First();
				var newCompilation = compilation.AddSyntaxTrees(cu);
				semanticModel = newCompilation.GetSemanticModel(cu);
				return new Tuple<CommonCompilation, ISemanticModel, NamespaceDeclarationSyntaxInfo>(
					newCompilation, semanticModel, namespaceNode);
			}
			if (!compilation.ContainsSyntaxTree(namespaceNode.Syntax.SyntaxTree))
			{
				compilation = compilation.AddSyntaxTrees(namespaceNode.Syntax.SyntaxTree);
			}
			semanticModel = compilation.GetSemanticModel(namespaceNode.Syntax.SyntaxTree);
			return new Tuple<CommonCompilation, ISemanticModel, NamespaceDeclarationSyntaxInfo>(
				compilation,
				semanticModel,
				namespaceNode);
		}

		private static IDictionary<string, NamespaceDeclaration> GetNamespaceDeclarations(IProject project, bool ignoreGeneratedCode = false)
		{
			return project.Documents
						  .Select(document => new { document, codeFile = document.FilePath })
						  .Where(@t => !ignoreGeneratedCode || !IsGeneratedCodeFile(t.document, Patterns))
						  .Select(@t => new { t, collector = new NamespaceCollectorSyntaxWalker() })
						  .Select(@t => new { t, syntaxRoot = t.@t.document.GetSyntaxRoot() })
						  .SelectMany(@t => t.@t.collector.GetNamespaces<NamespaceDeclarationSyntax>(t.syntaxRoot)
											   .Select(x => new NamespaceDeclarationSyntaxInfo
																{
																	Name = x.GetName(x.SyntaxTree.GetRoot()),
																	CodeFile = t.@t.@t.codeFile,
																	Syntax = x
																}))
						  .GroupBy(x => x.Name)
						  .ToDictionary(
							  x => x.Key,
							  y => new NamespaceDeclaration
									   {
										   Name = y.Key,
										   SyntaxNodes = y
									   });
		}

		private static bool IsGeneratedCodeFile(IDocument doc, IEnumerable<Regex> patterns)
		{
			var path = doc.FilePath;
			return !string.IsNullOrWhiteSpace(path) && patterns.Any(x => x.IsMatch(path));
		}

		private static IDictionary<string, TypeDeclaration> GetTypeDeclarations(NamespaceDeclaration namespaceDeclaration)
		{
			return namespaceDeclaration.SyntaxNodes
				.Select(namespaceNode => new
										 {
											 namespaceNode.Syntax,
											 node = namespaceNode
										 })
				.Select(@t =>
						{
							Func<TypeDeclarationSyntax, TypeDeclarationSyntaxInfo> selector =
								(x => new TypeDeclarationSyntaxInfo(t.node.CodeFile, x.SyntaxTree == null ? x.Identifier.ValueText : x.GetName(x.SyntaxTree.GetRoot()), x));
							return new
								   {
									   t,
									   selector
								   };
						})
				.Select(@t => new
							  {
								  t,
								  collector = new TypeCollectorSyntaxWalker()
							  })
				.SelectMany(
					@t =>
						t.collector.GetTypes<TypeDeclarationSyntax>(t.@t.@t.Syntax)
							.Select<TypeDeclarationSyntax, TypeDeclarationSyntaxInfo>(t.@t.selector))
				.GroupBy(x => x.Name)
				.ToDictionary(
					x => x.Key,
					y => new TypeDeclaration
						 {
							 Name = y.Key,
							 SyntaxNodes = y
						 });
		}

		private IProject GetDocuments(IProject project)
		{
			var doc = XDocument.Load(project.FilePath);
			var defaultNs = doc.Root.GetDefaultNamespace();
			var compiles = doc.Descendants(defaultNs + "Compile");
			var dependents = doc.Descendants(defaultNs + "DependentUpon");
			var filePaths =
				compiles
				   .Select(x => x.Attribute("Include").Value)
				   .Concat(dependents.Select(x => x.Value))
				   .OrderByDescending(x => x);

			project = filePaths.Aggregate(
				project,
				(p, s) =>
				{
					DocumentId did;
					var root = Path.GetDirectoryName(project.FilePath);
					var filepath = Path.Combine(root, s);
					var sourceCode = s.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
										 ? _converter.Convert(filepath)
										 : SyntaxTree.ParseFile(filepath);
					return p.AddDocument(s, sourceCode.GetText(), out did);
				});

			return project;
		}
	}
}