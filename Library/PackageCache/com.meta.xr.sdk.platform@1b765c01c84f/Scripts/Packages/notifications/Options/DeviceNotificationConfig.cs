// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<ca8bc8515e2072c274535e84e2726cea>>
 */

using System;
using System.Collections;
using Oculus.Platform.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using UnityEngine.Scripting;

namespace Oculus.Platform
{
    /// The Notification Params, to be passed in to
    /// Notifications.DeviceNotification, is a field that allows developers to
    /// specify a list of parameters to config the on device notification.
    [Serializable]
    [Preserve]
    public class DeviceNotificationConfig {
        /// The title for the notification.
        [JsonProperty("title")]
        [Preserve]
        public string Title = "";
        /// The message body for the notification.
        [JsonProperty("message")]
        [Preserve]
        public string Message = "";
        /// The attached image uri for the notification.
        [JsonProperty("media_attachment_uri")]
        [Preserve]
        public string MediaAttachmentUri = null;
        /// The notification delivery id used to track this notification. Make sure you
        /// never pass in same ndid for different notification instance, otherwise the
        /// notification won't be present.
        [JsonProperty("ndid")]
        [Preserve]
        public string Ndid = null;
        /// Indicates whether the notification should only be displayed as a toast. If
        /// true, the notification will not appear in notification feed.
        [JsonProperty("is_toast_only")]
        [Preserve]
        public bool IsToastOnly;
        /// This variable holds the app package name associated with the app icon shown
        /// in the notification body. It is only required when your app displays a
        /// notification with a different app icon.
        [JsonProperty("app_package_name_for_app_icon")]
        [Preserve]
        public string AppPackageNameForAppIcon = null;
        /// The notification action display type, which determines whether actions are
        /// displayed as ActionDisplayType.Iconable,
        /// ActionDisplayType.IconableColorless, or ActionDisplayType.TextOnly.
        [JsonProperty("action_display_type")]
        [JsonConverter(typeof(EnumDescriptionConverter<ActionDisplayType>))]
        [Preserve]
        public ActionDisplayType ActionDisplayType;
        /// The title for an optional notification action which will be used to open
        /// the app or perform another action when the action button is clicked.
        [JsonProperty("action_title")]
        [Preserve]
        public string ActionTitle = null;
        /// The icon enum that specifies the icon for an optional notification action
        /// which will be used to open the app or perform another action when the
        /// action button is clicked.
        [JsonProperty("action_icon")]
        [JsonConverter(typeof(EnumDescriptionConverter<ActionIcon>))]
        [Preserve]
        public ActionIcon ActionIcon;
        /// The app id for an optional notification action which will be used to open
        /// the specified app when the action button is clicked.
        [JsonProperty("action_app_id")]
        [Preserve]
        public string ActionAppId = null;
        /// The package name for an optional notification action which will be used to
        /// open the specified package when the action button is clicked.
        [JsonProperty("action_package_name")]
        [Preserve]
        public string ActionPackageName = null;
        /// The intent for an optional notification action which will be used to launch
        /// and intent with the specified intent data.
        [JsonProperty("action_intent_data")]
        [Preserve]
        public string ActionIntentData = null;
        /// The String JSON List of intent extras for an optional notification action
        /// which will be used to launch an intent with the specified intent extras.
        [JsonProperty("action_intent_extras")]
        [Preserve]
        public string ActionIntentExtras = null;
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public DeviceNotificationConfig()
        {
        }
        
        [Preserve]
        public void SetTitle(string value)
        {
            Title = value;
        }
        
        [Preserve]
        public void SetMessage(string value)
        {
            Message = value;
        }
        
        [Preserve]
        public void SetMediaAttachmentUri(string value)
        {
            MediaAttachmentUri = value;
        }
        
        [Preserve]
        public void SetNdid(string value)
        {
            Ndid = value;
        }
        
        [Preserve]
        public void SetIsToastOnly(bool value)
        {
            IsToastOnly = value;
        }
        
        [Preserve]
        public void SetAppPackageNameForAppIcon(string value)
        {
            AppPackageNameForAppIcon = value;
        }
        
        [Preserve]
        public void SetActionDisplayType(ActionDisplayType value)
        {
            ActionDisplayType = value;
        }
        
        [Preserve]
        public void SetActionTitle(string value)
        {
            ActionTitle = value;
        }
        
        [Preserve]
        public void SetActionIcon(ActionIcon value)
        {
            ActionIcon = value;
        }
        
        [Preserve]
        public void SetActionAppId(string value)
        {
            ActionAppId = value;
        }
        
        [Preserve]
        public void SetActionPackageName(string value)
        {
            ActionPackageName = value;
        }
        
        [Preserve]
        public void SetActionIntentData(string value)
        {
            ActionIntentData = value;
        }
        
        [Preserve]
        public void SetActionIntentExtras(string value)
        {
            ActionIntentExtras = value;
        }
        
        ~DeviceNotificationConfig()
        {
        }
    }
}
