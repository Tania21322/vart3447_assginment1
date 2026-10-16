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

using System;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// Abstraction over a push-to-talk dictation backend, so the DevAgent can select between the
    /// on-device System Intelligence recognizer and the Voice SDK at runtime. Implementations emit
    /// partial/final transcription and errors, and are driven by <see cref="Toggle(bool)"/>.
    /// </summary>
    internal interface IDictationSource
    {
        /// <summary>Fires repeatedly as speech is transcribed, with the running best-guess text.</summary>
        event Action<string> OnPartialTranscriptionUpdate;

        /// <summary>Fires once when a session stops, with the final transcript to send.</summary>
        event Action<string> OnTranscriptionFinalized;

        /// <summary>Fires when the dictation backend fails (permission, model, network, ...).</summary>
        event Action<string> OnDictationError;

        /// <summary>Toggles a dictation session based on the current active state.</summary>
        void Toggle();

        /// <summary>Starts (activate=true) or stops and finalizes (activate=false) a session.</summary>
        void Toggle(bool activate);

        /// <summary>Abandons the current session without emitting a final transcript.</summary>
        void Cancel();
    }
}
