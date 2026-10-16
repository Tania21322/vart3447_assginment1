// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<36f8970854412881347cd2e8543956bf>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// The IAP (In-App Purchases) API provides methods for managing in-app
    /// purchases, including retrieving purchase history, getting detailed product
    /// information by Purchase.Sku, and consuming purchases as needed. For more
    /// information, see
    /// [here](https://developer.oculus.com/documentation/unity/ps-iap/).
    public static partial class IAP
    {
        /// Allow the consumable IAP product to be purchased again. Conceptually, this
        /// indicates that the item was used or consumed. Important: Make sure to pass
        /// the correct SKU of the purchase that will be consumed. This value is case-
        /// sensitive and should match exactly with the product SKU set in the
        /// Developer Dashboard.
        public static Request ConsumePurchase(string sku)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "sku", sku }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("iap", "consume_purchase", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Retrieve a list of IAP products that can be purchased. Note: You must
        /// provide a list of SKUs (Stock Keeping Units) to retrieve the corresponding
        /// product information. The SKUs are used to identify the products in the
        /// Oculus store, which can be retrieved by accessing the Developer Dashboard
        /// or by Purchase.Sku.
        public static Request<ProductList> GetProductsBySKU(string[] skus)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "skus", skus }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("iap", "get_products_by_sku", 1, jsonRequest, 0);
                return new Request<ProductList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Retrieve a list of Purchase that the Logged-In-User has made. This list
        /// will also contain consumable purchases that have not been consumed. Note:
        /// This method returns all purchases, including consumable and non-consumable
        /// ones. If you only want to retrieve durable purchases, use
        /// get_viewer_purchases_durable_cache instead.
        public static Request<PurchaseList> GetViewerPurchases()
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("iap", "get_viewer_purchases", 1, jsonRequest, 0);
                return new Request<PurchaseList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Retrieve a list of Purchase that the Logged-In-User has made. This list
        /// will only contain durable purchase (non-consumable) and is populated from a
        /// device cache. Important: It is recommended to use IAP.GetViewerPurchases
        /// first and only check the cache if that fails. This method is intended as a
        /// fallback mechanism and may not always return up-to-date results.
        public static Request<PurchaseList> GetViewerPurchasesDurableCache()
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("iap", "get_viewer_purchases_durable_cache", 1, jsonRequest, 0);
                return new Request<PurchaseList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Launch the checkout flow to purchase the existing product. Oculus Home
        /// tries handle and fix as many errors as possible. Home returns the
        /// appropriate error message and how to resolve it, if possible. Returns a
        /// purchase on success, and an error on user cancellation or other errors. In
        /// the case of a user cancelation, the Error.Message value will contain a JSON
        /// object with a `"category"` property containing a value of
        /// `"user_canceled"`.
        public static Request<Purchase> LaunchCheckoutFlow(string sku)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "sku", sku }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("iap", "launch_checkout_flow", 1, jsonRequest, 0);
                return new Request<Purchase>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        

        public static Request<BillingPlanList> GetNextBillingPlanListPage(BillingPlanList list) {
            if (!list.HasNextPage)
            {
                Debug.LogWarning("Oculus.Platform.GetNextBillingPlanListPage: List has no next page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.NextUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("iap", "get_billing_plan_page_by_url", 1, jsonRequest, 0);
                return new Request<BillingPlanList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<ProductList> GetNextProductListPage(ProductList list) {
            if (!list.HasNextPage)
            {
                Debug.LogWarning("Oculus.Platform.GetNextProductListPage: List has no next page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.NextUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("iap", "get_product_page_by_url", 1, jsonRequest, 0);
                return new Request<ProductList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<PurchaseList> GetNextPurchaseListPage(PurchaseList list) {
            if (!list.HasNextPage)
            {
                Debug.LogWarning("Oculus.Platform.GetNextPurchaseListPage: List has no next page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.NextUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("iap", "get_purchase_page_by_url", 1, jsonRequest, 0);
                return new Request<PurchaseList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<TrialOfferList> GetNextTrialOfferListPage(TrialOfferList list) {
            if (!list.HasNextPage)
            {
                Debug.LogWarning("Oculus.Platform.GetNextTrialOfferListPage: List has no next page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.NextUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("iap", "get_trial_offer_page_by_url", 1, jsonRequest, 0);
                return new Request<TrialOfferList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<BillingPlanList> GetPreviousBillingPlanListPage(BillingPlanList list) {
            if (!list.HasPreviousPage)
            {
                Debug.LogWarning("Oculus.Platform.GetPreviousBillingPlanListPage: List has no previous page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.PreviousUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("iap", "get_billing_plan_page_by_url", 1, jsonRequest, 0);
                return new Request<BillingPlanList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<ProductList> GetPreviousProductListPage(ProductList list) {
            if (!list.HasPreviousPage)
            {
                Debug.LogWarning("Oculus.Platform.GetPreviousProductListPage: List has no previous page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.PreviousUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("iap", "get_product_page_by_url", 1, jsonRequest, 0);
                return new Request<ProductList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<PurchaseList> GetPreviousPurchaseListPage(PurchaseList list) {
            if (!list.HasPreviousPage)
            {
                Debug.LogWarning("Oculus.Platform.GetPreviousPurchaseListPage: List has no previous page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.PreviousUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("iap", "get_purchase_page_by_url", 1, jsonRequest, 0);
                return new Request<PurchaseList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<TrialOfferList> GetPreviousTrialOfferListPage(TrialOfferList list) {
            if (!list.HasPreviousPage)
            {
                Debug.LogWarning("Oculus.Platform.GetPreviousTrialOfferListPage: List has no previous page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.PreviousUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("iap", "get_trial_offer_page_by_url", 1, jsonRequest, 0);
                return new Request<TrialOfferList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }
    }
}
