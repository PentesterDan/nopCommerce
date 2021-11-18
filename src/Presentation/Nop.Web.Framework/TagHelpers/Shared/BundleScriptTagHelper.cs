using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Nop.Core;
using Nop.Core.Configuration;
using Nop.Web.Framework.Configuration;
using WebOptimizer;
using WebOptimizer.Extensions;

namespace Nop.Web.Framework.TagHelpers.Shared
{
    /// <summary>
    /// Script bundling tag helper
    /// </summary>
    [HtmlTargetElement(ASSET_TAG_NAME, Attributes = EXCLUDE_FROM_BUNDLE_ATTRIBUTE_NAME)]
    [HtmlTargetElement(ASSET_TAG_NAME, Attributes = SRC_ATTRIBUTE_NAME)] // we don't support the bundling of inline scripts yet
    [HtmlTargetElement(BUNDLE_TAG_NAME)]
    public class BundleScriptTagHelper : TagHelper
    {
        #region Constants

        private const string ASSET_TAG_NAME = "script";
        private const string BUNDLE_DESTINATION_KEY_NAME = "asp-bundle-dest-key";
        private const string BUNDLE_KEY_NAME = "asp-bundle-key";
        private const string BUNDLE_TAG_NAME = "script-bundle";
        private const string EXCLUDE_FROM_BUNDLE_ATTRIBUTE_NAME = "asp-exclude-from-bundle";
        private const string SRC_ATTRIBUTE_NAME = "src";

        #endregion

        #region Fields

        private readonly AppSettings _appSettings;
        private readonly IAssetPipeline _assetPipeline;

        #endregion

        #region Ctor

        public BundleScriptTagHelper(AppSettings appSettings, IAssetPipeline assetPipeline)
        {
            _appSettings = appSettings;
            _assetPipeline = assetPipeline ?? throw new ArgumentNullException(nameof(assetPipeline));
        }

        #endregion

        #region Methods

        public override Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            if (output == null)
                throw new ArgumentNullException(nameof(output));

            if (!output.Attributes.ContainsName("type")) // we don't touch other types e.g. text/template
                output.Attributes.SetAttribute("type", MimeTypes.TextJavascript);

            output.TagName = ASSET_TAG_NAME;
            output.TagMode = TagMode.StartTagAndEndTag;

            var bundleSuffix = _appSettings.Get<WebOptimizerConfig>().JavaScriptBundleSuffix;

            //to avoid collisions in controllers with the same names
            if (ViewContext.RouteData.Values.TryGetValue("area", out var area))
                bundleSuffix = $"{bundleSuffix}.{area}".ToLowerInvariant();

            if (string.Equals(context.TagName, BUNDLE_TAG_NAME))
                BundleDestinationKey ??= bundleSuffix;
            else
                BundleKey ??= bundleSuffix;
            
            var config = _appSettings.Get<WebOptimizerConfig>();

            if (!ExcludeFromBundle && config.EnableJsBundling)
                output.HandleJsBundle(_assetPipeline, ViewContext, config, Src, BundleKey, BundleDestinationKey);
            else
                output.Attributes.SetAttribute("src", Src);

            return Task.CompletedTask;
        }

        #endregion

        #region Properties

        /// <summary>
        /// A value indicating if a file should be excluded from the bundle
        /// </summary>
        [HtmlAttributeName(EXCLUDE_FROM_BUNDLE_ATTRIBUTE_NAME)]
        public bool ExcludeFromBundle { get; set; }

        /// <summary>
        /// The <see cref="ViewContext"/>.
        /// </summary>
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext { get; set; } = default;

        /// <summary>
        /// A key of a bundle to collect
        /// </summary>
        [HtmlAttributeName(BUNDLE_KEY_NAME)]
        public string BundleKey { get; set; }

        /// <summary>
        /// A key that defines the destination for the bundle.
        /// </summary>
        [HtmlAttributeName(BUNDLE_DESTINATION_KEY_NAME)]
        public string BundleDestinationKey { get; set; }

        /// <summary>
        /// Address of the external script to use
        /// </summary>
        [HtmlAttributeName(SRC_ATTRIBUTE_NAME)]
        public string Src { get; set; }

        #endregion

    }
}