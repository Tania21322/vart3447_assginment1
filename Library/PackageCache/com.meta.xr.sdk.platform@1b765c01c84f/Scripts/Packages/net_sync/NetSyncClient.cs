// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<470c85e2c01c68cb8d7242a555e7b713>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// It represents a network synchronization system that allows multiple clients
    /// to connect and communicate with each other in real-time. It provides a way
    /// to manage and facilitate real-time communication and data synchronization
    /// between multiple clients in a networked environment and it can be retrieved
    /// using NetSyncConnection.
    public static partial class NetSync
    {
        /// Creates a new connection to the given zone. If already connected to a zone,
        /// no new connection will be attempted.
        public static Request<NetSyncConnection> Connect(NetSyncOptions connectOptions)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connect_options", connectOptions }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "connect", 1, jsonRequest, 0);
                return new Request<NetSyncConnection>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Terminates a pending or active connection to a zone. Does nothing if there
        /// is no existing connection.
        public static Request<NetSyncConnection> Disconnect(long connectionId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "disconnect", 1, jsonRequest, 0);
                return new Request<NetSyncConnection>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Gets all the sessions (including those for the current context) that are
        /// connected to the same instance.
        public static Request<NetSyncSessionList> GetSessions(long connectionId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "get_sessions", 1, jsonRequest, 0);
                return new Request<NetSyncSessionList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Gets the current spatial voip attenuation values for default channel in a
        /// given connection
        public static Request<NetSyncVoipAttenuationValueList> GetVoipAttenuation(long connectionId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "get_voip_attenuation", 1, jsonRequest, 0);
                return new Request<NetSyncVoipAttenuationValueList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Gets the default spatial voip attenuation values
        public static Request<NetSyncVoipAttenuationValueList> GetVoipAttenuationDefault()
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

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "get_voip_attenuation_default", 1, jsonRequest, 0);
                return new Request<NetSyncVoipAttenuationValueList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Unimplemented, use set_voip_attenuation_model api instead
        public static Request SetVoipAttenuation(long connectionId, float[] distances, float[] decibels)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId },
                    { "distances", distances },
                    { "decibels", decibels }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "set_voip_attenuation", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Sets the spatial voip attenuation values for a given connection
        public static Request SetVoipAttenuationModel(long connectionId, string name, float[] distances, float[] decibels)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId },
                    { "name", name },
                    { "distances", distances },
                    { "decibels", decibels }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "set_voip_attenuation_model", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// creates or updates a channel to use the attn model specified for this
        /// session.
        public static Request SetVoipChannelCfg(long connectionId, string channelName, string attnmodel, bool disableSpatialization)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId },
                    { "channel_name", channelName },
                    { "attnmodel", attnmodel },
                    { "disable_spatialization", disableSpatialization }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "set_voip_channel_cfg", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// When mixing ambisonic streams, include audio only from other sessions that
        /// have the same value for voip group. The default group is specified by an
        /// empty string.
        public static Request SetVoipGroup(long connectionId, string groupId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId },
                    { "group_id", groupId }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "set_voip_group", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// The channel names this session is listening to. The channel name need to be
        /// defined before, otherwise default channel cfg will be used
        public static Request SetVoipListentoChannels(long connectionId, string[] listentoChannels)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId },
                    { "listento_channels", listentoChannels }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "set_voip_listento_channels", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Controls whether or not microphone data is sent from the this connection to
        /// the server, and where it is sourced from.
        public static Request SetVoipMicSource(long connectionId, NetSyncVoipMicSource micSource)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId },
                    { "mic_source", micSource.GetDescription() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "set_voip_mic_source", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// If true, the user associated with the given session is no longer included
        /// in the ambisonic stream, and that user's monostream(s) are disabled. Note -
        /// that if the user is connected via multiple devices (and thus has multiple
        /// sessions), all sessions belonging to that user will be muted. If the
        /// current user's session handle is provided, then the method will mute any
        /// other sessions connected under the same user. Will return false and perform
        /// no action if the given session does not exist.
        public static Request<NetSyncSetSessionPropertyResult> SetVoipSessionMuted(long connectionId, UInt64 sessionId, bool muted)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId },
                    { "session_id", sessionId.ToString() },
                    { "muted", muted }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "set_voip_session_muted", 1, jsonRequest, 0);
                return new Request<NetSyncSetSessionPropertyResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// The channel this session is speaking to. Note that a session can speak to a
        /// single channel at once. The speaking channel can be changed any time.
        public static Request SetVoipSpeaktoChannels(long connectionId, string[] speaktoChannels)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId },
                    { "speakto_channels", speaktoChannels }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "set_voip_speakto_channels", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Overrides the stream mode for the given user session. If Unknown is given,
        /// any previous overrides are discarded and the stream mode is set to the
        /// default as defined by the options given to the connection.
        public static Request<NetSyncSetSessionPropertyResult> SetVoipStreamMode(long connectionId, UInt64 sessionId, NetSyncVoipStreamMode streamMode)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId },
                    { "session_id", sessionId.ToString() },
                    { "stream_mode", streamMode.GetDescription() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "set_voip_stream_mode", 1, jsonRequest, 0);
                return new Request<NetSyncSetSessionPropertyResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Sent when the status of a connection has changed. The payload will be a
        /// type of NetSyncConnection.
        
        public static void SetConnectionStatusChangedNotificationCallback(Message<NetSyncConnection>.Callback callback)
        {
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new { });
                ulong sessionId = PlatformClient.MakeSession("net_sync", "connection_status_changed", 1, jsonRequest, 0);
                Callback.SetNotificationCallback<NetSyncConnection>(sessionId, callback);
            }
            else
            {
                Debug.LogError(Core.PlatformUninitializedError);
            }
        }

        /// Sent when the list of known connected sessions has changed. Contains the
        /// new list of sessions. The payload will be a type of
        /// NetSyncSessionsChangedNotification.
        
        public static void SetSessionsChangedNotificationCallback(Message<NetSyncSessionsChangedNotification>.Callback callback)
        {
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new { });
                ulong sessionId = PlatformClient.MakeSession("net_sync", "sessions_changed", 1, jsonRequest, 0);
                Callback.SetNotificationCallback<NetSyncSessionsChangedNotification>(sessionId, callback);
            }
            else
            {
                Debug.LogError(Core.PlatformUninitializedError);
            }
        }

        /// Gets all available samples of ambisonic mixed voice data and copies it into
        /// outputBuffer. Returns the number of frames copied. This function can be
        /// safely called from any thread.
        public static Request<UIntPtr> GetAmbisonicFloatPcm(long connectionId, float[] outputBuffer, UIntPtr outputBufferNumElements)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId },
                    { "output_buffer", outputBuffer },
                    { "output_buffer_num_elements", outputBufferNumElements }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "get_ambisonic_float_pcm", 1, jsonRequest, 0);
                return new Request<UIntPtr>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Gets all available samples of ambisonic mixed voice data and copies it into
        /// outputBuffer. Returns the number of frames copied. This function can be
        /// safely called from any thread.
        public static Request<UIntPtr> GetAmbisonicInt16Pcm(long connectionId, short[] outputBuffer, UIntPtr outputBufferNumElements)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId },
                    { "output_buffer", outputBuffer },
                    { "output_buffer_num_elements", outputBufferNumElements }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "get_ambisonic_int16_pcm", 1, jsonRequest, 0);
                return new Request<UIntPtr>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Gets all available samples of ambisonic mixed voice data and copies it into
        /// outputBuffer. Returns the number of frames copied. This function can be
        /// safely called from any thread.
        public static Request<UIntPtr> GetAmbisonicInterleavedFloatPcm(long connectionId, float[] outputBuffer, UIntPtr outputBufferNumElements)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId },
                    { "output_buffer", outputBuffer },
                    { "output_buffer_num_elements", outputBufferNumElements }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "get_ambisonic_interleaved_float_pcm", 1, jsonRequest, 0);
                return new Request<UIntPtr>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Gets all available samples of ambisonic mixed voice data and copies it into
        /// outputBuffer. Returns the number of frames copied. This function can be
        /// safely called from any thread.
        public static Request<UIntPtr> GetAmbisonicInterleavedInt16Pcm(long connectionId, short[] outputBuffer, UIntPtr outputBufferNumElements)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId },
                    { "output_buffer", outputBuffer },
                    { "output_buffer_num_elements", outputBufferNumElements }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "get_ambisonic_interleaved_int16_pcm", 1, jsonRequest, 0);
                return new Request<UIntPtr>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Populates the given 3 element array with the position of the given session.
        /// If the given session has never called set_listener_position, or if the
        /// position has not yet been replicated, this method will set each to { 0, 0,
        /// 0 } The coordinate system used for ambisonic mixing is as right handed:
        /// positive X pointing right, positive Y pointing upward, positive Z pointing
        /// backward A value of 1.0 is equal to 1 meter.
        public static Request<bool> GetListenerPosition(long connectionId, UInt64 sessionId, NetSyncVector3 position)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId },
                    { "session_id", sessionId.ToString() },
                    { "position", position }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "get_listener_position", 1, jsonRequest, 0);
                return new Request<bool>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Gets all available samples of monostream voice data for the given
        /// networking session identifier and copies it into outputBuffer. Returns the
        /// number of frames copied. This function can be safely called from any
        /// thread.
        public static Request<UIntPtr> GetMonostreamFloatPcm(long connectionId, UInt64 sessionId, float[] outputBuffer, UIntPtr outputBufferNumElements)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId },
                    { "session_id", sessionId.ToString() },
                    { "output_buffer", outputBuffer },
                    { "output_buffer_num_elements", outputBufferNumElements }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "get_monostream_float_pcm", 1, jsonRequest, 0);
                return new Request<UIntPtr>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Gets all available samples of monostream voice data for the given
        /// networking session identifier and copies it into outputBuffer. Returns the
        /// number of frames copied. This function can be safely called from any
        /// thread.
        public static Request<UIntPtr> GetMonostreamInt16Pcm(long connectionId, UInt64 sessionId, short[] outputBuffer, UIntPtr outputBufferNumElements)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId },
                    { "session_id", sessionId.ToString() },
                    { "output_buffer", outputBuffer },
                    { "output_buffer_num_elements", outputBufferNumElements }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "get_monostream_int16_pcm", 1, jsonRequest, 0);
                return new Request<UIntPtr>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Returns the maximum number of samples that an output buffer will need to
        /// hold.
        public static Request<UIntPtr> GetPcmBufferMaxSamples()
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

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "get_pcm_buffer_max_samples", 1, jsonRequest, 0);
                return new Request<UIntPtr>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// gets the voip amplitude of other sessions. the amplitude range is from
        /// [0.0f, 1.0f] when successfully set the function returns true, else false.
        public static Request<bool> GetVoipAmplitude(long connectionId, UInt64 sessionId, NetSyncVector3 amplitude)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId },
                    { "session_id", sessionId.ToString() },
                    { "amplitude", amplitude }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "get_voip_amplitude", 1, jsonRequest, 0);
                return new Request<bool>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Sets the position of the listener for spatialized audio, which is used to
        /// mix the ambisonic stream on the server. This is also used as the position
        /// for the microphone.
        public static Request SetListenerPosition(long connectionId, NetSyncVector3 position)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "connection_id", connectionId },
                    { "position", position }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "set_listener_position", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        

        public static Request<NetSyncSessionList> GetNextNetSyncSessionListPage(NetSyncSessionList list) {
            if (!list.HasNextPage)
            {
                Debug.LogWarning("Oculus.Platform.GetNextNetSyncSessionListPage: List has no next page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.NextUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "get_net_sync_session_page_by_url", 1, jsonRequest, 0);
                return new Request<NetSyncSessionList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<NetSyncVoipAttenuationValueList> GetNextNetSyncVoipAttenuationValueListPage(NetSyncVoipAttenuationValueList list) {
            if (!list.HasNextPage)
            {
                Debug.LogWarning("Oculus.Platform.GetNextNetSyncVoipAttenuationValueListPage: List has no next page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.NextUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "get_net_sync_voip_attenuation_value_page_by_url", 1, jsonRequest, 0);
                return new Request<NetSyncVoipAttenuationValueList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<NetSyncSessionList> GetPreviousNetSyncSessionListPage(NetSyncSessionList list) {
            if (!list.HasPreviousPage)
            {
                Debug.LogWarning("Oculus.Platform.GetPreviousNetSyncSessionListPage: List has no previous page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.PreviousUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "get_net_sync_session_page_by_url", 1, jsonRequest, 0);
                return new Request<NetSyncSessionList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<NetSyncVoipAttenuationValueList> GetPreviousNetSyncVoipAttenuationValueListPage(NetSyncVoipAttenuationValueList list) {
            if (!list.HasPreviousPage)
            {
                Debug.LogWarning("Oculus.Platform.GetPreviousNetSyncVoipAttenuationValueListPage: List has no previous page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.PreviousUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("net_sync", "get_net_sync_voip_attenuation_value_page_by_url", 1, jsonRequest, 0);
                return new Request<NetSyncVoipAttenuationValueList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }
    }
}
