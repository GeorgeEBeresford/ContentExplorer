using System.Configuration;
using System.IO;
using System.Web.Mvc;

namespace ContentExplorer.Controllers
{
    public class ImageController : Controller
    {
        [HttpGet]
        public ActionResult Index(string path = "", int? page = 1, string filter = null)
        {
            if (page == null || page < 1)
            {
                page = 1;
            }

            if (path == null)
            {
                path = "";
            }
            else
            {
                // Remove any dangerous characters to prevent escaping the current context
                path = path.Trim("/\\.".ToCharArray());
            }

            if (filter == null)
            {
                filter = "";
            }

            DirectoryInfo directoryInfo = GetCurrentDirectory(path);

            if (directoryInfo.Exists == false)
            {
                return RedirectToAction("Index", new { page, filter });
            }

            ViewBag.FilesPerPage = 50;
            ViewBag.Directory = directoryInfo;
            ViewBag.Page = page;
            ViewBag.Filter = filter;

            return View();
        }

        [HttpGet]
        public ViewResult View(string path, int page, string filter = "")
        {
            if (page < 1)
            {
                page = 1;
            }

            if (filter == null)
            {
                filter = "";
            }

            ViewBag.Path = path;
            ViewBag.Id = page;
            ViewBag.Filter = filter;


            // Zero-based index
            int pageIndex = page - 1;

            int startingPreview = pageIndex - 7 < 1 ? 1 : page - 7;
            ViewBag.StartingPreview = startingPreview;

            return View();
        }

        private DirectoryInfo GetCurrentDirectory(string relativePath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(Path.Combine(ConfigurationManager.AppSettings["BaseDirectory"], ConfigurationManager.AppSettings["ImagesPath"], relativePath));

            return directoryInfo;
        }
    }
}