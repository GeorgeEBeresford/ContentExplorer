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
                "~/Core/Styles/site.min.css"));
        }

        private static void RegisterScripts(BundleCollection bundles)
        {
            bundles.Add(new ScriptBundle("~/bundles/scripts/common").Include(
                "~/Core/Scripts/Common/jquery-{version}.js",
                "~/Core/Scripts/Common/modernizr-*",
                "~/Core/Scripts/Common/bootstrap.js",
                "~/Core/Scripts/Common/HttpRequester.js",
                "~/Core/Scripts/Common/MediaRepository.js",
                "~/Core/Scripts/Common/TagRepository.js",
                "~/Core/Scripts/Common/FileIndex.js"
            ));

            bundles.Add(new ScriptBundle("~/bundles/scripts/image/view").Include(
                "~/Core/Scripts/Image/View.js"));

            bundles.Add(new ScriptBundle("~/bundles/scripts/image/index").Include(
                "~/Core/Scripts/Image/Index.js"));

            bundles.Add(new ScriptBundle("~/bundles/scripts/video/view").Include(
                "~/Core/Scripts/Video/View.js"));

            bundles.Add(new ScriptBundle("~/bundles/scripts/video/index").Include(
                "~/Core/Scripts/Video/Index.js"));
        }
    }
}
