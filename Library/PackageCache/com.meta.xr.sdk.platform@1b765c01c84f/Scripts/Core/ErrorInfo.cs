// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Oculus.Platform
{
    /// <summary>
    /// Model for deserializing error information from the statusMessage JSON.
    /// The JSON structure from the server looks like:
    /// {
    ///   "error": {
    ///     "is_graph_api_error": true,
    ///     "http_code": 400,
    ///     "message": "Error message here",
    ///     "type": "OAuthException",
    ///     "code": 1005,
    ///     "error_subcode": 1891065,
    ///     "is_transient": false,
    ///     "error_user_title": "Error Title",
    ///     "error_user_msg": "User-friendly error message.",
    ///     "fbtrace_id": "trace_id_here"
    ///   }
    /// }
    /// </summary>
    [Preserve]
    internal class ErrorInfoWrapper
    {
        [JsonProperty("error")]
        [Preserve]
        public ErrorInfo Error { get; set; }
    }

    /// <summary>
    /// Represents the inner error object containing detailed error information.
    /// </summary>
    [Preserve]
    internal class ErrorInfo
    {
        [JsonProperty("is_graph_api_error")]
        [Preserve]
        public bool IsGraphApiError { get; set; }

        [JsonProperty("http_code")]
        [Preserve]
        public int HttpCode { get; set; } = -1;

        [JsonProperty("message")]
        [Preserve]
        public string Message { get; set; }

        [JsonProperty("type")]
        [Preserve]
        public string Type { get; set; }

        [JsonProperty("code")]
        [Preserve]
        public int Code { get; set; }

        [JsonProperty("error_subcode")]
        [Preserve]
        public int ErrorSubcode { get; set; }

        [JsonProperty("is_transient")]
        [Preserve]
        public bool IsTransient { get; set; }

        [JsonProperty("error_user_title")]
        [Preserve]
        public string ErrorUserTitle { get; set; }

        [JsonProperty("error_user_msg")]
        [Preserve]
        public string ErrorUserMsg { get; set; }

        [JsonProperty("fbtrace_id")]
        [Preserve]
        public string FbtraceId { get; set; }
    }
}
