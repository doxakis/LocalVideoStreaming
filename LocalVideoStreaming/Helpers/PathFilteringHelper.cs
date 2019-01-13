using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LocalVideoStreaming.Helpers
{
	public interface IPathFilteringHelper
	{
		string RootPath { get; }
		string EnsureValidPathOrReturnRootPath(string path);
	}

	public class PathFilteringHelper : IPathFilteringHelper
	{
		private readonly IConfiguration _configuration;
		private const string RootPathMediaKey = "RootPathMedia";

		public PathFilteringHelper(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public string RootPath => _configuration.GetValue<string>(RootPathMediaKey);

		public string EnsureValidPathOrReturnRootPath(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
				path = RootPath;
			foreach (var invalidPart in new string[] { ".." })
				if (path.Contains(invalidPart))
					path = RootPath;
			if (!path.StartsWith(RootPath))
				path = RootPath;

			return path;
		}
	}
}
