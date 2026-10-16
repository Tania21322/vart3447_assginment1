// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<4596d1dae0a657a8cca02d6182981b9e>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// This class provides methods to access information about the User. It allows
    /// you to retrieve a user's ID, access token, and org-scoped ID, as well as
    /// their friends list and recently met users. Additionally, it provides
    /// methods to launch various flows such as blocking, unblocking, reporting,
    /// and sending friend requests. It's useful when you need to manage user
    /// relationships or perform actions that require user authentication within
    /// your application.
    public static partial class Users
    {
        /// Retrieve the user with the given ID. This might fail if the ID is invalid
        /// or the user is blocked. NOTE: Users will have a unique ID per application.
        public static Request<User> Get(UInt64 userID)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "user_id", userID.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("users", "get", 1, jsonRequest, 0);
                return new Request<User>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Return an access token string for this user, suitable for making REST calls
        /// against graph.oculus.com.
        public static Request<string> GetAccessToken()
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

                ulong requestHandle = PlatformClient.MakeRequest("users", "get_access_token", 1, jsonRequest, 0);
                return new Request<string>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Return the IDs of users entitled to use the current app that are blocked by
        /// the specified user
        public static Request<BlockedUserList> GetBlockedUsers()
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

                ulong requestHandle = PlatformClient.MakeRequest("users", "get_blocked_users", 1, jsonRequest, 0);
                return new Request<BlockedUserList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Returns a list of linked accounts that are associated with the specified
        /// service providers. Customization can be done via UserOptions. Create this
        /// object with UsersOptions.ServiceProvider.add - returns the list of linked
        /// accounts that are associated with these specified service providers.
        /// Example custom C++ usage: {{code_samples[0]}}
        public static Request<LinkedAccountList> GetLinkedAccounts(UserOptions userOptions)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "user_options", userOptions }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("users", "get_linked_accounts", 1, jsonRequest, 0);
                return new Request<LinkedAccountList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Retrieve the currently signed in user. This call is available offline.
        /// NOTE: Please be aware that this function will only return the following
        /// information about the user: Alias (Oculus ID), ID (App Scoped ID), Profile
        /// URL (image_url). If you need to retrieve additional user information, such
        /// as presence details, please use the App Scoped ID obtained from
        /// Users.GetLoggedInUser() in conjunction with Users.Get(ulong UserID). This
        /// will provide access to more comprehensive user data. Retrieve the currently
        /// signed in user.  This call is available offline. NOTE: This will not return
        /// the user's presence as it should always be 'online' in your application.
        /// NOTE: Users will have a unique ID per application.
        public static Request<User> GetLoggedInUser()
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

                ulong requestHandle = PlatformClient.MakeRequest("users", "get_logged_in_user", 1, jsonRequest, 0);
                return new Request<User>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Retrieve a list of the logged in user's bidirectional followers. The
        /// payload type will be an array of User A list of the logged in user's
        /// bidirectional followers.
        public static Request<UserList> GetLoggedInUserFriends()
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

                ulong requestHandle = PlatformClient.MakeRequest("users", "get_logged_in_user_friends", 1, jsonRequest, 0);
                return new Request<UserList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Retrieve the currently signed in user's managed info.  This call is not
        /// available offline. NOTE: This will return data only if the logged in user
        /// is a managed Meta account (MMA).
        public static Request<User> GetLoggedInUserManagedInfo()
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

                ulong requestHandle = PlatformClient.MakeRequest("users", "get_logged_in_user_managed_info", 1, jsonRequest, 0);
                return new Request<User>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// returns an ovrID which is unique per org. allows different apps within the
        /// same org to identify the user.
        public static Request<OrgScopedID> GetOrgScopedID(UInt64 userID)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "user_id", userID.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("users", "get_org_scoped_id", 1, jsonRequest, 0);
                return new Request<OrgScopedID>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Returns all accounts belonging to this user. Accounts are the Oculus user
        /// and x-users that are linked to this user.
        public static Request<SdkAccountList> GetSdkAccounts()
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

                ulong requestHandle = PlatformClient.MakeRequest("users", "get_sdk_accounts", 1, jsonRequest, 0);
                return new Request<SdkAccountList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Fetches all the UserCapability for the logged-in user
        public static Request<UserCapabilityList> GetUserCapabilities()
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

                ulong requestHandle = PlatformClient.MakeRequest("users", "get_user_capabilities", 1, jsonRequest, 0);
                return new Request<UserCapabilityList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Part of the scheme to confirm the identity of a particular user in your
        /// backend. You can pass the result of Users.GetUserProof and a user ID from
        /// User.Id to your backend. Your server can then use our api to verify
        /// identity. 'https://graph.oculus.com/user_nonce_validate?nonce=USER_PROOF&am
        /// p;user_id=USER_ID&amp;access_token=ACCESS_TOKEN' NOTE: The nonce is only
        /// good for one check and then it is invalidated.
        public static Request<UserProof> GetUserProof()
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

                ulong requestHandle = PlatformClient.MakeRequest("users", "get_user_proof", 1, jsonRequest, 0);
                return new Request<UserProof>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Launch the Avacam Settings activity used to configure the appearance of the
        /// Avatar Camera.
        public static Request<LaunchAvacamSettingsResult> LaunchAvacamSettings()
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

                ulong requestHandle = PlatformClient.MakeRequest("users", "launch_avacam_settings", 1, jsonRequest, 0);
                return new Request<LaunchAvacamSettingsResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Launch the flow for blocking the given user. You can't follow, be followed,
        /// invited, or searched by a blocked user, for example. You can remove the
        /// block via ovr_User_LaunchUnblockFlow.
        public static Request<LaunchBlockFlowResult> LaunchBlockFlow(UInt64 userID)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "user_id", userID.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("users", "launch_block_flow", 1, jsonRequest, 0);
                return new Request<LaunchBlockFlowResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Launch the flow for sending a follow request to a user.
        public static Request<LaunchFriendRequestFlowResult> LaunchFriendRequestFlow(UInt64 userID)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "user_id", userID.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("users", "launch_friend_request_flow", 1, jsonRequest, 0);
                return new Request<LaunchFriendRequestFlowResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Launch the flow for unblocking a user that the viewer has blocked.
        public static Request<LaunchUnblockFlowResult> LaunchUnblockFlow(UInt64 userID)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "user_id", userID.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("users", "launch_unblock_flow", 1, jsonRequest, 0);
                return new Request<LaunchUnblockFlowResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Allows apps to initiate a login flow using the Meta Horizon App on a mobile
        /// device. The auth_url will be sent to the Horizon Mobile App and the user
        /// will be redirected to the auth_url to complete the login flow.
        public static Request<AndroidJavaObject> SendAuthUrl(string authUrl)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "auth_url", authUrl }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("users", "send_auth_url", 1, jsonRequest, 0);
                return new Request<AndroidJavaObject>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Return a new omni test user on a temporary db. Because these are transient,
        /// these users will eventually be automatically cleaned up. These users should
        /// only be used in tests.
        public static Request<string> NewTestUser()
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

                ulong requestHandle = PlatformClient.MakeRequest("users", "new_test_user", 1, jsonRequest, 0);
                return new Request<string>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Return an array of omni test user on a temporary db. Because these are
        /// transient, these users will eventually be automatically cleaned up. These
        /// users should only be used in tests. These two users are friends.
        public static Request<string> NewTestUserFriends()
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

                ulong requestHandle = PlatformClient.MakeRequest("users", "new_test_user_friends", 1, jsonRequest, 0);
                return new Request<string>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        

        public static Request<BlockedUserList> GetNextBlockedUserListPage(BlockedUserList list) {
            if (!list.HasNextPage)
            {
                Debug.LogWarning("Oculus.Platform.GetNextBlockedUserListPage: List has no next page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.NextUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("users", "get_blocked_user_page_by_url", 1, jsonRequest, 0);
                return new Request<BlockedUserList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<LinkedAccountList> GetNextLinkedAccountListPage(LinkedAccountList list) {
            if (!list.HasNextPage)
            {
                Debug.LogWarning("Oculus.Platform.GetNextLinkedAccountListPage: List has no next page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.NextUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("users", "get_linked_account_page_by_url", 1, jsonRequest, 0);
                return new Request<LinkedAccountList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<SdkAccountList> GetNextSdkAccountListPage(SdkAccountList list) {
            if (!list.HasNextPage)
            {
                Debug.LogWarning("Oculus.Platform.GetNextSdkAccountListPage: List has no next page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.NextUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("users", "get_sdk_account_page_by_url", 1, jsonRequest, 0);
                return new Request<SdkAccountList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<UserList> GetNextUserListPage(UserList list) {
            if (!list.HasNextPage)
            {
                Debug.LogWarning("Oculus.Platform.GetNextUserListPage: List has no next page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.NextUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("users", "get_user_page_by_url", 1, jsonRequest, 0);
                return new Request<UserList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<UserCapabilityList> GetNextUserCapabilityListPage(UserCapabilityList list) {
            if (!list.HasNextPage)
            {
                Debug.LogWarning("Oculus.Platform.GetNextUserCapabilityListPage: List has no next page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.NextUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("users", "get_user_capability_page_by_url", 1, jsonRequest, 0);
                return new Request<UserCapabilityList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<BlockedUserList> GetPreviousBlockedUserListPage(BlockedUserList list) {
            if (!list.HasPreviousPage)
            {
                Debug.LogWarning("Oculus.Platform.GetPreviousBlockedUserListPage: List has no previous page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.PreviousUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("users", "get_blocked_user_page_by_url", 1, jsonRequest, 0);
                return new Request<BlockedUserList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<LinkedAccountList> GetPreviousLinkedAccountListPage(LinkedAccountList list) {
            if (!list.HasPreviousPage)
            {
                Debug.LogWarning("Oculus.Platform.GetPreviousLinkedAccountListPage: List has no previous page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.PreviousUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("users", "get_linked_account_page_by_url", 1, jsonRequest, 0);
                return new Request<LinkedAccountList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<SdkAccountList> GetPreviousSdkAccountListPage(SdkAccountList list) {
            if (!list.HasPreviousPage)
            {
                Debug.LogWarning("Oculus.Platform.GetPreviousSdkAccountListPage: List has no previous page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.PreviousUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("users", "get_sdk_account_page_by_url", 1, jsonRequest, 0);
                return new Request<SdkAccountList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<UserList> GetPreviousUserListPage(UserList list) {
            if (!list.HasPreviousPage)
            {
                Debug.LogWarning("Oculus.Platform.GetPreviousUserListPage: List has no previous page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.PreviousUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("users", "get_user_page_by_url", 1, jsonRequest, 0);
                return new Request<UserList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<UserCapabilityList> GetPreviousUserCapabilityListPage(UserCapabilityList list) {
            if (!list.HasPreviousPage)
            {
                Debug.LogWarning("Oculus.Platform.GetPreviousUserCapabilityListPage: List has no previous page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.PreviousUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("users", "get_user_capability_page_by_url", 1, jsonRequest, 0);
                return new Request<UserCapabilityList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }
    }
}
