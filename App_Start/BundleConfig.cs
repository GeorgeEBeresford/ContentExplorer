using System.Web;
using System.Web.Optimization;

namespace ContentExplorer
{
    public class BundleConfig
    {
        // For more information on bundling, visit https://go.microsoft.com/fwlink/?LinkId=301862
        public static void RegisterBundles(BundleCollection bundles)
        {
            RegisterStyles(bundles);
            RegisterScripts(bundles);
        }

        private static void RegisterStyles(BundleCollection bundles)
        {
            bundles.Add(new StyleBundle("~/Content/css").Include(
                "~/Content/bootstrap.css",
                "~/Content/site.min.css"));
        }

        private static void RegisterScripts(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/jquery").Include(
                "~/Scripts/Common/jquery-{version}.js"));

            bundles.Add(new ScriptBundle("~/bundles/jqueryval").Include(
                "~/Scripts/Common/jquery.validate*"));

            // Use the development version of Modernizr to develop with and learn from. Then, when you're
            // ready for production, use the build tool at https://modernizr.com to pick only the tests you need.
            bundles.Add(new ScriptBundle("~/bundles/modernizr").Include(
                "~/Scripts/Common/modernizr-*"));

            bundles.Add(new ScriptBundle("~/bundles/bootstrap").Include(
                "~/Scripts/Common/bootstrap.js"));

            bundles.Add(new ScriptBundle("~/bundles/image/view").Include(
                "~/Scripts/Image/View.js"));

            bundles.Add(new ScriptBundle("~/bundles/image/index").Include(
                "~/Scripts/Image/Index.js"));

            bundles.Add(new ScriptBundle("~/bundles/video/view").Include(
                "~/Scripts/Video/View.js"));

            bundles.Add(new ScriptBundle("~/bundles/video/index").Include(
                "~/Scripts/Video/Index.js"));
        }
    }
}
