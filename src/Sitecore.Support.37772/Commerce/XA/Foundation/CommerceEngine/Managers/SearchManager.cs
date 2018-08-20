namespace Sitecore.Support.Commerce.XA.Foundation.CommerceEngine.Managers
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using ContentSearch;

    using ContentSearch.Linq;
    using Data.Items;
    using Diagnostics;
    using Sitecore.Commerce.Engine.Connect;
    using Sitecore.Commerce.Engine.Connect.Interfaces;
    using Sitecore.Commerce.Engine.Connect.Search;
    using Sitecore.Commerce.XA.Foundation.CommerceEngine.Search;
    using Sitecore.Commerce.XA.Foundation.Common.Context;
    using Sitecore.Commerce.XA.Foundation.Common.Search;

    public class SearchManager : Sitecore.Commerce.XA.Foundation.CommerceEngine.Managers.SearchManager
    {
        public SearchManager([NotNull] IStorefrontContext storefrontContext, [NotNull] IContext context)
            : base(storefrontContext, context)
        {
        }

        [SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        public override SearchResults SearchCatalogItemsByKeyword(string keyword, string catalogName,
            CommerceSearchOptions searchOptions)
        {
            Assert.ArgumentNotNullOrEmpty(catalogName, "catalogName");
            Assert.ArgumentNotNull(searchOptions, nameof(searchOptions));

            var returnList = new List<Item>();
            var totalPageCount = 0;
            var totalProductCount = 0;
            IEnumerable<CommerceQueryFacet> facets = null;

            var searchManager = CommerceTypeLoader.CreateInstance<ICommerceSearchManager>();
            var searchIndex = searchManager.GetIndex(catalogName);
            var startCategoryPath = StorefrontContext.CurrentStorefront.GetStartNavigationCategoryItem().Paths.Path;
            var catalogId = StorefrontContext.CurrentStorefront.CatalogItem.ID.ToGuid().ToString();

            using (var context = searchIndex.CreateSearchContext())
            {
                var query = context.GetQueryable<CommerceSellableItemSearchResultItem>()
                    .Where(
                        item => item.Name.Equals(keyword) || item.DisplayName.Equals(keyword)
                                                          || item.Content.Contains(keyword))
                    .Where(
                        item => item.CommerceSearchItemType == CommerceSearchItemType.SellableItem
                                || item.CommerceSearchItemType == CommerceSearchItemType.Category)
                    .Where(item => item.Path.StartsWith(startCategoryPath))
                    .Where(item => item.Language == CurrentLanguageName)
                    .Where(item => item.ParentCatalogList.Contains(catalogId))
                    .Where(item => item[BuiltinFields.LatestVersion].Equals("1"));

                var csSearchOptions = SearchHelper.ToCommerceServerSearchOptions(searchOptions);
                query = searchManager.AddSearchOptionsToQuery(query,
                    csSearchOptions);
                var results = query.GetResults();
                var response = SearchResponse.CreateFromSearchResultsItems(csSearchOptions, results);

                if (response != null)
                {
                    returnList.AddRange(response.ResponseItems);
                    totalProductCount = response.TotalItemCount;
                    totalPageCount = response.TotalPageCount;
                    facets = SearchHelper.ToCommerceFacets(response.Facets);
                }

                var searchResults = new SearchResults(returnList, totalProductCount, totalPageCount,
                    searchOptions.StartPageIndex, facets);

                return searchResults;
            }
        }
    }
}