using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LocalVideoStreaming.Helpers;
using Microsoft.AspNetCore.Mvc;

namespace LocalVideoStreaming.Controllers
{
    public class BrowseController : Controller
    {
		public IPathFilteringHelper PathFilteringHelper { get; }

		public BrowseController(IPathFilteringHelper pathFilteringHelper)
		{
			PathFilteringHelper = pathFilteringHelper;
		}

		public class IndexModel
		{
			public DirectoryEntryModel Parent { get; set; }
			public List<DirectoryEntryModel> Folders { get; set; }
			public List<DirectoryEntryModel> Video { get; set; }
		}

		public class DirectoryEntryModel
		{
			public string Shortname { get; set; }
			public string Path { get; set; }
		}

		public IActionResult Index(string path)
        {
			path = PathFilteringHelper.EnsureValidPathOrReturnRootPath(path);

			var model = new IndexModel
			{
				Parent = path == PathFilteringHelper.RootPath
					? null
					: new DirectoryEntryModel
					{
						Path = Directory.GetParent(path).FullName,
						Shortname = Directory.GetParent(path).Name
					},
				Folders = Directory.EnumerateDirectories(path)
					.Select(folder => new DirectoryEntryModel
					{
						Path = folder,
						Shortname = Path.GetFileName(folder)
					})
					.ToList(),
				Video = Directory.EnumerateFiles(path, "*.mp4", SearchOption.TopDirectoryOnly)
					.OrderBy(file => file, new NaturalStringComparer())
					.Select(file => new DirectoryEntryModel
					{
						Path = file,
						Shortname = Path.GetFileNameWithoutExtension(file)
					})
					.ToList()
			};

            return View(model);
        }
    }
}