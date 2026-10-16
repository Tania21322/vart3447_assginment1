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

using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// Defines how the filter should behave when the active state is true and the tag is present.
    /// </summary>
    public enum TagFilterMode
    {
        /// <summary>
        /// Exclude tagged objects when active state is true (deny-list pattern).
        /// </summary>
        Exclude,

        /// <summary>
        /// Include only tagged objects when active state is true (allow-list pattern).
        /// </summary>
        Include
    }

    /// <summary>
    /// A state-aware filter that conditionally includes or excludes interactables based on an <see cref="IActiveState"/>.
    /// Can operate in two modes: Exclude (deny-list) or Include (allow-list).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Exclude Mode (default):</b> When the active state is true, interactables tagged with the specified tag are filtered out.
    /// Common use case: Implementing graceful fallback from Gaze to Ray interactions.
    /// When attached to a <see cref="RayInteractor"/> with a <see cref="GazeActiveState"/>,
    /// the Ray interactor is blocked from interacting with tagged interactables when gaze tracking
    /// is available. When gaze tracking becomes unavailable, those interactables become accessible
    /// to the Ray interactor.
    /// </para>
    /// <para>
    /// <b>Include Mode:</b> When the active state is true, only interactables tagged with the specified tag are allowed.
    /// Common use case: Progressive disclosure or conditional access patterns.
    /// For example, only allow interaction with objects tagged "TutorialStep2" when that step is active.
    /// </para>
    /// <para>
    /// This component is generic and works with any <see cref="IActiveState"/> implementation,
    /// enabling various conditional interaction patterns.
    /// </para>
    /// <para>
    /// Typical setup for Gaze/Ray fallback (Exclude mode):
    /// <list type="bullet">
    /// <item>Add this component to a RayInteractor GameObject</item>
    /// <item>Assign a GazeActiveState to the Active State field</item>
    /// <item>Set Mode to Exclude (default)</item>
    /// <item>On any RayInteractable that should fallback from Gaze, add a TagSet component
    /// with the GAZE_RAY_FALLBACK_TAG</item>
    /// </list>
    /// </para>
    /// </remarks>
    [Experimental]
    public class ActiveStateTagSetFilter : MonoBehaviour, IGameObjectFilter
    {
        [SerializeField]
        [Tooltip("The active state to observe. When active, the filter mode determines how tagged objects are handled. Typically a GazeActiveState.")]
        [Interface(typeof(IActiveState))]
        private UnityEngine.Object _activeState;

        [SerializeField]
        [Tooltip("Exclude: Block tagged objects when active (deny-list). Include: Allow only tagged objects when active (allow-list).")]
        private TagFilterMode _mode = TagFilterMode.Exclude;

        [SerializeField]
        [Tooltip("Tag to check on target interactables. Behavior depends on filter mode setting.")]
        private string _tag = "";

        private IActiveState ActiveState;

        /// <summary>
        /// Gets or sets the tag to check on target interactables.
        /// Behavior depends on the filter mode setting.
        /// </summary>
        public string Tag
        {
            get => _tag;
            set => _tag = value;
        }

        protected virtual void Awake()
        {
            if (ActiveState == null)
            {
                ActiveState = _activeState as IActiveState;
            }
        }

        protected virtual void Start()
        {
            this.AssertField(ActiveState, nameof(ActiveState));
        }

        /// <summary>
        /// Filters a GameObject based on the current state, mode, and presence of the tag.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When the active state is inactive, all GameObjects pass the filter (return true).
        /// </para>
        /// <para>
        /// When the active state is active:
        /// <list type="bullet">
        /// <item><b>Exclude mode:</b> GameObjects with the tag are excluded (return false), others pass (return true)</item>
        /// <item><b>Include mode:</b> Only GameObjects with the tag pass (return true), others are excluded (return false)</item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <param name="target">The GameObject to filter</param>
        /// <returns>
        /// True if the GameObject should be included in interaction, false if it should
        /// be excluded (filtered out)
        /// </returns>
        public bool Filter(GameObject target)
        {
            // When state is inactive, allow all objects
            if (ActiveState == null || !ActiveState.Active)
            {
                return true;
            }

            if (string.IsNullOrEmpty(_tag))
            {
                return true;
            }

            bool hasTag = false;
            if (target.TryGetComponent(out TagSet tagSet))
            {
                hasTag = tagSet.ContainsTag(_tag);
            }

            // Apply filter based on mode
            switch (_mode)
            {
                case TagFilterMode.Exclude:
                    // Exclude mode: Block objects WITH the tag when active
                    return !hasTag;

                case TagFilterMode.Include:
                    // Include mode: Allow ONLY objects WITH the tag when active
                    return hasTag;

                default:
                    return true;
            }
        }

        #region Inject

        /// <summary>
        /// Sets all required fields for a dynamically instantiated ActiveStateTagSetFilter.
        /// This method exists to support Interaction SDK's dependency injection pattern
        /// and is not needed for typical Unity Editor-based usage.
        /// </summary>
        /// <param name="activeState">The active state to observe</param>
        public void InjectAllActiveStateTagSetFilter(IActiveState activeState)
        {
            InjectActiveState(activeState);
        }

        /// <summary>
        /// Sets the active state in a dynamically instantiated ActiveStateTagSetFilter.
        /// This method exists to support Interaction SDK's dependency injection pattern
        /// and is not needed for typical Unity Editor-based usage.
        /// </summary>
        public void InjectActiveState(IActiveState activeState)
        {
            _activeState = activeState as UnityEngine.Object;
            ActiveState = activeState;
        }

        #endregion
    }
}
