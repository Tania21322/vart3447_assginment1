// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<7af62ac2f8711f09fdbab9cbd58923d0>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// The voip API provides platform methods to interact with the voip
    /// connections, inputs, syncing, and systems. You can also retrieve different
    /// statuses for the current voip state. These methods exist to help integrate
    /// a platform solution for voip in your app. Read more about voip in our
    /// [docs](https://developer.oculus.com/documentation/unity/ps-parties/)
    public static partial class Voip
    {
        /// Gets whether the microphone is currently available to the app. This can be
        /// used to show if the user's voice is able to be heard by other users.
        /// Returns a microphone availability state flag which determines whether it is
        /// available or not - MicrophoneAvailabilityState.
        public static Request<MicrophoneAvailabilityState> GetMicrophoneAvailability()
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

                ulong requestHandle = PlatformClient.MakeRequest("voip", "get_microphone_availability", 1, jsonRequest, 0);
                return new Request<MicrophoneAvailabilityState>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Indicates that the current microphone availability state has been updated.
        /// Use Voip.GetMicrophoneAvailability to extract the microphone availability
        /// state.
        
        public static void SetMicrophoneAvailabilityStateUpdateNotificationCallback(Message<String>.Callback callback)
        {
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new { });
                ulong sessionId = PlatformClient.MakeSession("voip", "microphone_availability_state_update", 1, jsonRequest, 0);
                Callback.SetNotificationCallback<String>(sessionId, callback);
            }
            else
            {
                Debug.LogError(Core.PlatformUninitializedError);
            }
        }

        /// Sent to indicate that some part of the overall state of SystemVoip has
        /// changed. Use SystemVoipState.Status and the properties of SystemVoipState
        /// to extract the state that triggered the notification. Note that the state
        /// may have changed further since the notification was generated, and that you
        /// may call the `GetSystemVoip...()` family of functions at any time to get
        /// the current state directly.
        
        public static void SetSystemVoipStateNotificationCallback(Message<SystemVoipState>.Callback callback)
        {
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new { });
                ulong sessionId = PlatformClient.MakeSession("voip", "system_voip_state", 1, jsonRequest, 0);
                Callback.SetNotificationCallback<SystemVoipState>(sessionId, callback);
            }
            else
            {
                Debug.LogError(Core.PlatformUninitializedError);
            }
        }

        /// Accepts a VoIP connection from a given user.
        public static Request AcceptRequest(UInt64 userId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "user_id", userId.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("voip", "accept", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Gets whether or not a voice connection is using discontinuous transmission
        /// (DTX). Both sides must set to using DTX when their connection is
        /// established in order for this to be true. Returns unknown if there is no
        /// connection.
        public static Request<VoipDtxState> GetIsConnectionUsingDtxRequest(UInt64 peerId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "peer_id", peerId.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("voip", "get_is_connection_using_dtx", 1, jsonRequest, 0);
                return new Request<VoipDtxState>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Gets the current local bitrate used for the connection to the specified
        /// user.  This is set by the current client. Returns unknown if there is no
        /// connection.
        public static Request<VoipBitrate> GetLocalBitrateRequest(UInt64 peerId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "peer_id", peerId.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("voip", "get_local_bitrate", 1, jsonRequest, 0);
                return new Request<VoipBitrate>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Returns the size of the internal ringbuffer used by the voip system in
        /// elements.  This size is the maximum number of elements that can ever be
        /// returned by Voip.GetPcm. This function can be safely called from any
        /// thread.
        public static Request<UIntPtr> GetOutputBufferMaxSize()
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

                ulong requestHandle = PlatformClient.MakeRequest("voip", "get_output_buffer_max_size", 1, jsonRequest, 0);
                return new Request<UIntPtr>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Returns the current number of audio samples available to read for the
        /// specified user. This function is inherently racy; it's possible that more
        /// data can be added between a call to this function and a subsequent call to
        /// Voip.GetPcm. This function can be safely called from any thread.
        public static Request<UIntPtr> GetPcmSize(UInt64 senderId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "sender_id", senderId.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("voip", "get_pcm_size", 1, jsonRequest, 0);
                return new Request<UIntPtr>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Gets the current remote bitrate used for the connection to the specified
        /// user.  This is set by the client on the other side of the connection.
        /// Returns unknown if there is no connection.
        public static Request<VoipBitrate> GetRemoteBitrateRequest(UInt64 peerId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "peer_id", peerId.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("voip", "get_remote_bitrate", 1, jsonRequest, 0);
                return new Request<VoipBitrate>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Returns a timestamp used for synchronizing audio samples sent to the given
        /// user with an external data stream. Timestamps associated with audio frames
        /// are implicitly transmitted to remote peers; on the receiving side, they can
        /// be obtained by using Voip.GetPcmWithTimestamp.  Voip.GetPcmWithTimestamp is
        /// used to fetch those timestamps on the sending side -- an application can
        /// insert the value returned by this function into each data packet and
        /// compare it to the value returned by GetPCMWithTimestamp() on the receiving
        /// side in order to determine the ordering of two events (sampling audio and
        /// composing a data packet). Note: the timestamp is generated by an
        /// unspecified clock; it's doesn't generally represent wall-clock time.  Use
        /// Voip.GetSyncTimestampDifference to convert the difference between two
        /// timestamps to microseconds. This function assumes that a voice connection
        /// to the user already exists; it returns 0 if that isn't the case.
        public static Request<UInt32> GetSyncTimestamp(UInt64 userId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "user_id", userId.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("voip", "get_sync_timestamp", 1, jsonRequest, 0);
                return new Request<UInt32>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Calculates the difference between two sync timestamps, returned by either
        /// Voip.GetSyncTimestamp or Voip.GetPcmWithTimestamp, and converts it to
        /// microseconds. Return value will be negative if lhs is smaller than rhs,
        /// zero if both timestamps are the same, and positive otherwise.  The absolute
        /// value of the result is the time in microseconds between two sync
        /// timestamps.
        public static Request<long> GetSyncTimestampDifference(UInt32 lhs, UInt32 rhs)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "lhs", lhs },
                    { "rhs", rhs }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("voip", "get_sync_timestamp_difference", 1, jsonRequest, 0);
                return new Request<long>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Returns SystemVoip microphone's mute state.
        public static Request<VoipMuteState> GetSystemVoipMicrophoneMutedRequest()
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

                ulong requestHandle = PlatformClient.MakeRequest("voip", "get_system_voip_microphone_muted", 1, jsonRequest, 0);
                return new Request<VoipMuteState>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Returns SystemVoip status.
        public static Request<SystemVoipStatus> GetSystemVoipStatusRequest()
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

                ulong requestHandle = PlatformClient.MakeRequest("voip", "get_system_voip_status", 1, jsonRequest, 0);
                return new Request<SystemVoipStatus>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// This function is used to enable or disable the local microphone.  When
        /// muted, the microphone will not transmit any audio. Voip connections are
        /// unaffected by this state.  New connections can be established or closed
        /// whether the microphone is muted or not. This can be used to implement push-
        /// to-talk, or a local mute button.  The default state is unmuted. This
        /// function can be safely called from any thread.
        public static Request SetMicrophoneMutedRequest(VoipMuteState state)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "state", state.GetDescription() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("voip", "set_microphone_muted", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// The options set for newly created connections to use. Existing connections
        /// will continue to use their current settings until they are destroyed and
        /// recreated.
        public static Request SetNewConnectionOptionsRequest(VoipOptions voipOptions)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "voip_options", voipOptions }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("voip", "set_new_connection_options", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Sets the output sample rate.  Audio data will be resampled as it is placed
        /// into the internal ringbuffer. This function can be safely called from any
        /// thread.
        public static Request SetOutputSampleRate(VoipSampleRate rate)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "rate", rate.GetDescription() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("voip", "set_output_sample_rate", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Attempts to establish a VoIP session with the specified user. A message of
        /// type Voip.SystemVoipState will be posted when the session is established.
        /// This function can be safely called from any thread.
        public static Request StartRequest(UInt64 userId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "user_id", userId.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("voip", "start", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Terminates a VoIP session with the specified user.  Note that a muting
        /// functionality should be used to temporarily stop sending audio; restarting
        /// a VoIP session after tearing it down may be an expensive operation. This
        /// function can be safely called from any thread.
        public static Request StopRequest(UInt64 userId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "user_id", userId.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("voip", "stop", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        
    }
}
