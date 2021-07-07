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
                "~/Core/Scripts/Common/Repositories/MediaRepository.js",
                "~/Core/Scripts/Common/Repositories/TagRepository.js",
                "~/Core/Scripts/Common/Factories/MediaUiFactory.js"
            ));

            bundles.Add(new ScriptBundle("~/bundles/scripts/image/view")
                .Include(
                    "~/Core/Scripts/View/MediaView.js",
                    "~/Core/Scripts/View/ImageView.js"
                )
            );

            bundles.Add(new ScriptBundle("~/bundles/scripts/video/view")
                .Include(
                    "~/Core/Scripts/View/MediaView.js",
                    "~/Core/Scripts/View/VideoView.js"
                )
            );

            bundles.Add(new ScriptBundle("~/bundles/scripts/image/index")
                .Include(
                    "~/Core/Scripts/Index/MediaIndex.js",
                    "~/Core/Scripts/Index/ImageIndex.js"
                )
            );

            bundles.Add(new ScriptBundle("~/bundles/scripts/video/index")
                .Include(
                    "~/Core/Scripts/Index/MediaIndex.js",
                    "~/Core/Scripts/Index/VideoIndex.js"
                )
            );
        }
    }
}
