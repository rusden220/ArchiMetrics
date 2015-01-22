namespace ArchiMetrics.Analysis.Metrics
{
	using System.Collections.Generic;
	using ArchiMetrics.Common;
	using ArchiMetrics.Common.Metrics;

	internal class MemberDocumentation : IMemberDocumentation
	{
		public MemberDocumentation(string summary, string code, string example, string remarks, string returns, IEnumerable<TypeParameterDocumentation> typeParameters, IEnumerable<ParameterDocumentation> parameters, IEnumerable<ExceptionDocumentation> exceptions)
		{
			Summary = summary;
			Code = code;
			Example = example;
			Remarks = remarks;
			Returns = returns;
			TypeParameters = typeParameters.AsArray();
			Parameters = parameters.AsArray();
			Exceptions = exceptions.AsArray();
		}

		public string Summary { get; private set; }

		public string Code { get; private set; }

		public string Example { get; private set; }

		public string Remarks { get; private set; }

		public string Returns { get; private set; }

		public IEnumerable<TypeParameterDocumentation> TypeParameters { get; private set; }

		public IEnumerable<ParameterDocumentation> Parameters { get; private set; }

		public IEnumerable<ExceptionDocumentation> Exceptions { get; private set; }
	}
}