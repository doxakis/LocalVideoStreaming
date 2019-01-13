using LocalVideoStreaming.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;

namespace LocalVideoStreaming.Controllers
{
	public class PlayerController : Controller
    {
		public IPathFilteringHelper PathFilteringHelper { get; }

		public PlayerController(IPathFilteringHelper pathFilteringHelper)
		{
			PathFilteringHelper = pathFilteringHelper;
		}

		public class VideoModel
		{
			public string Shortname { get; set; }
			public string Path { get; set; }
			public string PreviousVideoPath { get; set; }
			public string NextVideoPath { get; set; }
			public DirectoryEntryModel Parent { get; set; }
		}

		public class DirectoryEntryModel
		{
			public string Shortname { get; set; }
			public string Path { get; set; }
		}

		public IActionResult Index(string path)
        {
			path = PathFilteringHelper.EnsureValidPathOrReturnRootPath(path);
			if (path == PathFilteringHelper.RootPath)
				return NotFound();

			var videosNearby = Directory.EnumerateFiles(
				Directory.GetParent(path).FullName,
				"*.mp4",
				SearchOption.TopDirectoryOnly)
				.OrderBy(file => file, new NaturalStringComparer())
				.ToList();

			var model = new VideoModel
			{
				Parent = new DirectoryEntryModel
				{
					Path = Directory.GetParent(path).FullName,
					Shortname = Directory.GetParent(path).Name
				},
				Path = path,
				Shortname = Path.GetFileNameWithoutExtension(path)
			};

			int currentPositionInFolder = videosNearby.IndexOf(path);
			if (currentPositionInFolder > 0)
				model.PreviousVideoPath = videosNearby[currentPositionInFolder - 1];
			if (currentPositionInFolder < videosNearby.Count - 1)
				model.NextVideoPath = videosNearby[currentPositionInFolder + 1];

			return View(model);
        }

		public IActionResult Stream(string path)
		{
			path = PathFilteringHelper.EnsureValidPathOrReturnRootPath(path);

			var fileContentType = "application/octet-stream";
			return File(System.IO.File.OpenRead(path), fileContentType, true);
		}
	}
}