using Sitecore.Commerce.XA.Foundation.Common;
using Sitecore.Commerce.XA.Foundation.Common.Search;
using Sitecore.Commerce.Engine.Connect;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Commerce.Engine.Connect.Interfaces;
using static Sitecore.Commerce.XA.Foundation.Common.Constants;
using Sitecore.Commerce.Engine.Connect.Search;
using Sitecore.Commerce.XA.Foundation.CommerceEngine.Search;
using Sitecore.ContentSearch.Linq;

namespace Sitecore.Support.Commerce.XA.Foundation.CommerceEngine.Managers
{
    public class SearchManager : Sitecore.Commerce.XA.Foundation.CommerceEngine.Managers.SearchManager
    {
        public SearchManager([NotNull]IStorefrontContext storefrontContext)
    : base(storefrontContext)
        {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
        public override SearchResults SearchCatalogItemsByKeyword(string keyword, string catalogName, CommerceSearchOptions searchOptions)
        {
            Assert.ArgumentNotNullOrEmpty(catalogName, "catalogName");

            var returnList = new List<Item>();
            var totalPageCount = 0;
            var totalProductCount = 0;
            IEnumerable<CommerceQueryFacet> facets = null;

            var searchManager = CommerceTypeLoader.CreateInstance<ICommerceSearchManager>();
            var searchIndex = searchManager.GetIndex(catalogName);
            var startCategory = this.StorefrontContext.CurrentStorefront.GetStartNavigationCategory();
            var startCategoryIsDepartmentLevel = startCategory?.ToString() == CatalogConstants.CatalogConfiguration.DepartmentsCatalogItemId;

            using (var context = searchIndex.CreateSearchContext())
            {
                IQueryable<CommerceSellableItemSearchResultItem> csSearchResults;

                if (startCategoryIsDepartmentLevel)
                {
                    csSearchResults = context.GetQueryable<CommerceSellableItemSearchResultItem>()
                       .Where(item =>
                           item.Name.Equals(keyword) || item["_displayname"].Equals(keyword) ||
                           item.Content.Contains(keyword))
                       .Where(item =>
                           item.CommerceSearchItemType == CommerceSearchItemType.SellableItem ||
                           item.CommerceSearchItemType == CommerceSearchItemType.Category)

                       // .Where(item => item.CatalogEntityId == catalogName)
                       .Where(item => item.Language == this.CurrentLanguageName)
                       .Select(p => new CommerceSellableItemSearchResultItem()
                       {
                           ItemId = p.ItemId,
                           Uri = p.Uri
                       });
                }
                else
                {
                    csSearchResults = context.GetQueryable<CommerceSellableItemSearchResultItem>()
                        .Where(item =>
                            item.Name.Equals(keyword) || item["_displayname"].Equals(keyword) ||
                            item.Content.Contains(keyword))
                        .Where(item =>
                            item.CommerceSearchItemType == CommerceSearchItemType.SellableItem ||
                            item.CommerceSearchItemType == CommerceSearchItemType.Category)
                    #region start modified part of the code
                        //CommerceAncestorIds index field doesn't contain all ancestors. It contains only parent (direct ancestor).
                        //_path field contains all ancestor, therefore the code has been modified to use Paths property
                        //.Where(item => item.CommerceAncestorIds.Contains(startCategory))
                        .Where(item => item.Paths.Contains(startCategory))
                    #endregion end of the modified part of code
                        // .Where(item => item.CatalogEntityId == catalogName)
                        .Where(item => item.Language == this.CurrentLanguageName)
                        .Select(p => new CommerceSellableItemSearchResultItem()
                        {
                            ItemId = p.ItemId,
                            Uri = p.Uri
                        });
                }

                var csSearchOptions = SearchHelper.ToCommerceServerSearchOptions(searchOptions);
                csSearchResults = searchManager.AddSearchOptionsToQuery<CommerceSellableItemSearchResultItem>(csSearchResults, csSearchOptions);

                var results = csSearchResults.GetResults();
                var response = SearchResponse.CreateFromSearchResultsItems(csSearchOptions, results);

                if (response != null)
                {
                    returnList.AddRange(response.ResponseItems);

                    totalProductCount = response.TotalItemCount;
                    totalPageCount = response.TotalPageCount;
                    facets = SearchHelper.ToCommerceFacets(response.Facets);
                }

                var searchResults = new SearchResults(returnList, totalProductCount, totalPageCount, searchOptions.StartPageIndex, facets);

                return searchResults;
            }
        }
    }
}
