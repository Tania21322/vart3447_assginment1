/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Collections;
using System.Threading;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// <see cref="IDictationSource"/> backed by on-device System Intelligence ASR (the Android
    /// <c>SpeechRecognizer</c>). Push-to-talk: <see cref="Toggle(bool)"/> true starts, false finalizes.
    ///
    /// Two behaviours match what the Quest system-keyboard dictation does with the same SI service:
    ///  - ONE recognizer is created and kept warm for the component's lifetime. Recreating it per
    ///    session lets the SI service unload the model, so every session pays the cold-start again.
    ///    Sessions end with <c>cancel()</c> (or a natural endpoint), never <c>stopListening</c> — the
    ///    SI service ignores stopListening ("recognition not active") and leaves the session half-open,
    ///    which wedges the next start with "already in session"/ERROR_CLIENT.
    ///  - The recognizer is single-utterance: it endpoints after a pause (~2 sentences) and stops.
    ///    For continuous dictation we RESTART it on each endpoint while push-to-talk is held, appending
    ///    each segment, and only finalize when the user releases.
    ///
    /// The SI recognizer streams non-final partials and does not deliver a final onResults on release,
    /// so on release we wait a brief grace for the tail partial and finalize from the latest partial.
    /// A busy/failed start self-heals by recreating the recognizer once.
    /// </summary>
    internal class SiDictationController : MonoBehaviour, IDictationSource
    {
        // android.speech.SpeechRecognizer error codes.
        private const int ErrorSpeechTimeout = 6;
        private const int ErrorNoMatch = 7;
        private const int ErrorRecognizerBusy = 8;
        private const int ErrorLanguageUnavailable = 13;
        private const string Language = "en-US";
        private const float TailGraceSeconds = 0.8f;
        private const float BusyRetrySeconds = 0.6f;

        private enum State { Idle, Listening, Finishing }

        public event Action<string> OnPartialTranscriptionUpdate;
        public event Action<string> OnTranscriptionFinalized;
        public event Action<string> OnDictationError;

        private readonly DictationTranscriptAccumulator _accumulator = new DictationTranscriptAccumulator();
        private AndroidJavaObject _activity;
        private AndroidJavaObject _recognizer;
        private SiRecognitionListener _listener;
        private SynchronizationContext _unityContext;

        private State _state = State.Idle;
        private bool _busyRetried;
        private bool _modelDownloadRequested;
        private string _lastPartial = "";
        private Coroutine _watchdog;

        internal static SiDictationController CreateAsChild(GameObject parent)
        {
            var go = new GameObject("[Voice] SI Dictation");
            go.transform.SetParent(parent.transform, false);
            return go.AddComponent<SiDictationController>();
        }

        private void Awake()
        {
            _unityContext = SynchronizationContext.Current;
            _activity = SiAsr.GetActivity();
            SiAsr.RequestMicPermission();

            _listener = new SiRecognitionListener(_unityContext);
            _listener.OnReady += HandleReady;
            _listener.OnPartial += HandlePartial;
            _listener.OnFinal += HandleFinal;
            _listener.OnError += HandleError;
        }

        public void Toggle()
        {
            Toggle(_state != State.Listening);
        }

        public void Toggle(bool activate)
        {
            if (activate)
            {
                StartSession();
            }
            else
            {
                StopSession();
            }
        }

        public void Cancel()
        {
            StopWatchdog();
            _accumulator.Reset();
            _lastPartial = "";
            _state = State.Idle;
            ResetRecognizer();
        }

        private void StartSession()
        {
            StopWatchdog();
            _state = State.Listening;
            _busyRetried = false;
            _accumulator.Reset();
            _lastPartial = "";
            BeginListening();
        }

        private void StopSession()
        {
            if (_state != State.Listening)
            {
                return;
            }

            _state = State.Finishing;
            // SI never sends a final onResults on release — keep listening a brief grace to catch the
            // tail partial (partials lag speech by ~1s), then finalize from the latest partial.
            _watchdog = StartCoroutine(FinalizeAfterGrace());
        }

        // Starts (or restarts) recognition on the warm, reused recognizer — on the Android UI thread.
        private void BeginListening()
        {
            SiAsr.RunOnUiThread(_activity, () =>
            {
                try
                {
                    EnsureRecognizerUiThread();
                    using var intent = BuildIntent();
                    _recognizer.Call("startListening", intent);
                }
                catch (Exception e)
                {
                    Post(() => HandleStartFailure(e));
                }
            });
        }

        // Must run on the Android UI thread. Creates the recognizer once and keeps it warm.
        private void EnsureRecognizerUiThread()
        {
            if (_recognizer == null)
            {
                _recognizer = SiAsr.CreateSiRecognizer(_activity);
                _recognizer.Call("setRecognitionListener", _listener);
            }
        }

        // Builds the recognition intent with long silence thresholds so the recognizer streams through
        // natural pauses instead of endpointing mid-dictation. Each endpoint restarts the recognizer
        // and drops the words spoken during the endpoint-detection + restart window.
        private AndroidJavaObject BuildIntent()
        {
            var intent = SiAsr.BuildMicIntent(Language, true);
            PutIntExtra(intent, "android.speech.extra.SPEECH_INPUT_COMPLETE_SILENCE_LENGTH_MILLIS", 15000);
            PutIntExtra(intent, "android.speech.extra.SPEECH_INPUT_POSSIBLY_COMPLETE_SILENCE_LENGTH_MILLIS", 15000);
            return intent;
        }

        private static void PutIntExtra(AndroidJavaObject intent, string key, int value)
        {
            using var _ = intent.Call<AndroidJavaObject>("putExtra", key, value);
        }

        // Callbacks below arrive already marshalled onto the Unity main thread by SiRecognitionListener.

        private void HandleReady()
        {
            // A ready callback with no active session (e.g. a stale one after finalize) — release it.
            // Note: _busyRetried is intentionally not reset here (only when a new session begins), so
            // a busy recognizer self-heals at most once per session rather than looping.
            if (_state == State.Idle)
            {
                ResetRecognizer();
            }
        }

        private void HandlePartial(string text)
        {
            if (_state == State.Idle)
            {
                return;
            }

            _lastPartial = text ?? "";
            OnPartialTranscriptionUpdate?.Invoke(_accumulator.OnPartial(_lastPartial));
        }

        // onResults = the SI recognizer endpointed this utterance. During a held session that means a
        // sentence boundary, not the end of dictation, so commit the segment and keep going.
        private void HandleFinal(string text)
        {
            if (_state == State.Idle)
            {
                return;
            }

            CommitSegment(!string.IsNullOrEmpty(text) ? text : _lastPartial);

            if (_state == State.Listening)
            {
                BeginListening();
            }
            else
            {
                FinalizeAndReset(_accumulator.GetFinalTranscript());
            }
        }

        private void HandleError(int code)
        {
            StopWatchdog();

            if (_state == State.Idle)
            {
                // Warmup or an already-finished session — nothing to recover.
                ResetRecognizer();
                return;
            }

            // A busy/dropped recognizer self-heals: recreate it fresh and try once more.
            if (code == ErrorRecognizerBusy && !_busyRetried)
            {
                _busyRetried = true;
                StartCoroutine(RecreateAndListenAfter(BusyRetrySeconds));
                return;
            }

            // Endpoint with no result / waiting-for-speech timeout. While the user still holds PTT,
            // just restart to keep capturing; if they've released, finalize what we have.
            if (code == ErrorNoMatch || code == ErrorSpeechTimeout)
            {
                CommitSegment(_lastPartial);
                if (_state == State.Listening)
                {
                    BeginListening();
                }
                else
                {
                    FinalizeAndReset(_accumulator.GetFinalTranscript());
                }
                return;
            }

            _state = State.Idle;

            if (code == ErrorLanguageUnavailable)
            {
                RequestModelDownload();
                _accumulator.Reset();
                _lastPartial = "";
                ResetRecognizer();
                OnDictationError?.Invoke($"On-device speech model for {Language} is downloading — try again shortly.");
                return;
            }

            _accumulator.Reset();
            _lastPartial = "";
            ResetRecognizer();
            OnDictationError?.Invoke($"Speech recognition error {code}");
        }

        private void CommitSegment(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                _accumulator.CommitFull(text);
            }

            _lastPartial = "";
        }

        private IEnumerator RecreateAndListenAfter(float delay)
        {
            yield return new WaitForSeconds(delay);

            // State can change during the wait: the user may have released PTT (Finishing) or the
            // session may have ended (Idle). Only recreate + restart while still actively listening.
            if (_state != State.Listening)
            {
                if (_state == State.Finishing)
                {
                    FinalizeAndReset(_accumulator.GetFinalTranscript());
                }
                yield break;
            }

            SiAsr.RunOnUiThread(_activity, () =>
            {
                DestroyRecognizerUiThread();
                try
                {
                    EnsureRecognizerUiThread();
                    using var intent = BuildIntent();
                    _recognizer.Call("startListening", intent);
                }
                catch (Exception e)
                {
                    Post(() => HandleStartFailure(e));
                }
            });
        }

        private void HandleStartFailure(Exception e)
        {
            _state = State.Idle;
            StopWatchdog();
            TeardownRecognizer();
            OnDictationError?.Invoke($"Could not start on-device speech recognition: {e.Message}");
        }

        private void FinalizeAndReset(string text)
        {
            StopWatchdog();
            _state = State.Idle;
            _lastPartial = "";
            ResetRecognizer();
            OnTranscriptionFinalized?.Invoke(text);
        }

        private IEnumerator FinalizeAfterGrace()
        {
            yield return new WaitForSeconds(TailGraceSeconds);
            if (_state == State.Finishing)
            {
                FinalizeAndReset(_accumulator.GetFinalTranscript());
            }
        }

        private void StopWatchdog()
        {
            if (_watchdog != null)
            {
                StopCoroutine(_watchdog);
                _watchdog = null;
            }
        }

        private void RequestModelDownload()
        {
            if (_modelDownloadRequested)
            {
                return;
            }

            _modelDownloadRequested = true;
            RunRecognizer(r =>
            {
                using var intent = SiAsr.BuildMicIntent(Language, true);
                r.Call("triggerModelDownload", intent);
            });
        }

        private void RunRecognizer(Action<AndroidJavaObject> action)
        {
            var recognizer = _recognizer;
            if (recognizer == null)
            {
                return;
            }

            SiAsr.RunOnUiThread(_activity, () =>
            {
                try
                {
                    action(recognizer);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Speech recognizer operation failed: {e.Message}");
                }
            });
        }

        // Ends the current recognition session but keeps the recognizer bound so the model stays warm.
        private void ResetRecognizer()
        {
            RunRecognizer(r => r.Call("cancel"));
        }

        private void TeardownRecognizer()
        {
            SiAsr.RunOnUiThread(_activity, DestroyRecognizerUiThread);
        }

        // Must run on the Android UI thread.
        private void DestroyRecognizerUiThread()
        {
            var recognizer = _recognizer;
            _recognizer = null;
            if (recognizer == null)
            {
                return;
            }

            try
            {
                recognizer.Call("cancel");
                recognizer.Call("destroy");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Speech recognizer teardown failed: {e.Message}");
            }
            finally
            {
                recognizer.Dispose();
            }
        }

        private void OnDestroy()
        {
            StopWatchdog();
            TeardownRecognizer();
        }

        private void Post(Action action)
        {
            if (_unityContext != null)
            {
                _unityContext.Post(_ => action(), null);
            }
            else
            {
                action();
            }
        }
    }
}
#endif
