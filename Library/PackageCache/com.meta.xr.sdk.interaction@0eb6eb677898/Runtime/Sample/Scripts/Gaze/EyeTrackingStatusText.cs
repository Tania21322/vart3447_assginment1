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

using TMPro;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// Simple component to display eye tracking hardware status in a TextMeshPro text field.
    /// Useful for debugging and displaying the current eye tracking state.
    /// </summary>
    public class EyeTrackingStatusText : MonoBehaviour
    {
        [SerializeField]
        [Interface(typeof(IActiveState))]
        private UnityEngine.Object _eyeTrackingActiveState;

        private IActiveState EyeTrackingActiveState => _eyeTrackingActiveState as IActiveState;

        [SerializeField]
        [Tooltip("The TextMeshPro text component to update with status.")]
        private TMP_Text _text;

        [SerializeField]
        [Tooltip("Text to display when eye tracking is active.")]
        private string _activeText = "Eye Tracking: <color=green>Active</color>";

        [SerializeField]
        [Tooltip("Text to display when eye tracking is inactive.")]
        private string _inactiveText = "Eye Tracking: <color=yellow>Inactive</color>";

        private void Update()
        {
            if (EyeTrackingActiveState != null && _text != null)
            {
                _text.text = EyeTrackingActiveState.Active
                    ? _activeText
                    : _inactiveText;
            }
        }
    }
}
