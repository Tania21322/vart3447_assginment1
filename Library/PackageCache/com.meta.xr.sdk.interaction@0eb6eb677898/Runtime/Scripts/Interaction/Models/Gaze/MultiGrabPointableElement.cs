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
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// An <see cref="IPointableElement"/> that wraps another <see cref="IPointableElement"/> and remaps
    /// selecting points so a multi-handed grab pivots about this element's transform with no visual jump
    /// as grabs are added or removed.
    /// </summary>
    /// <remarks>
    /// While two or more pointers select, a constant offset maps each finger-tip pose to a forwarded pose
    /// whose centroid lands on the center; the offset is recomputed on every grab-count change and
    /// surviving grabs are re-based via synthetic unselect/select pairs. The added or released grab is
    /// sequenced before/after the rebase loop so the downstream selecting count never falls below
    /// min(oldCount, newCount) mid-transition.
    /// </remarks>
    [Experimental]
    public class MultiGrabPointableElement : IPointableElement, IDisposable
    {
        /// <summary>
        /// Resolves the finger-tip (origin) pose for a selecting <see cref="IInteractorView"/>. Returns
        /// <c>false</c> when no origin pose is available, in which case the incoming provider pose is used.
        /// </summary>
        public delegate bool TryGetOriginPose(IInteractorView interactorView, out Pose pose);

        // The interactable that owns this element. Multi-grab points are sourced from its selecting
        // interactors' origin (finger-tip) poses, supplied by the injected delegate, rather than the
        // incoming pose, so two-grab scaling works with any movement provider.
        private readonly IInteractableView _interactableView;

        // The remap center about which multi-handed grabs pivot and scale.
        private readonly Transform _center;

        // Resolves each selecting interactor's origin (finger-tip) pose for the centroid/remap math.
        private readonly TryGetOriginPose _tryGetOriginPose;

        /// <summary>
        /// The resolved downstream <see cref="IPointableElement"/> that this element forwards to.
        /// </summary>
        public IPointableElement ForwardElement { get; private set; }

        /// <summary>
        /// Raised with the original (hand) event after the downstream element has been driven.
        /// </summary>
        public event Action<PointerEvent> WhenPointerEventRaised = delegate { };

        // A selecting grab: the latest incoming (provider-mapped) pose and the latest pose forwarded
        // downstream (so re-bases can issue a no-op move at the current downstream pose). The finger-tip
        // used for the centroid/remap math is sourced live from the interactor; see HandPose.
        private struct Grab
        {
            public int Id;
            public Pose Provider;
            public Pose Forwarded;
            public object Data;

            public Grab(int id, Pose provider, object data)
            {
                Id = id;
                Provider = provider;
                Forwarded = provider;
                Data = data;
            }
        }

        private readonly List<Grab> _grabs = new List<Grab>();

        // Remap offset; zero while the selecting count is <= 1.
        private Vector3 _offset = Vector3.zero;

        /// <summary>
        /// Creates a multi-grab wrapper that forwards remapped events to <paramref name="forwardElement"/>,
        /// observes the state of <paramref name="interactable"/>, pivots multi-handed grabs about
        /// <paramref name="center"/>, and sources finger-tips via <paramref name="tryGetOriginPose"/>.
        /// </summary>
        /// <param name="forwardElement">The downstream element that remapped events are forwarded to.</param>
        /// <param name="interactable">The interactable whose state and selecting interactors drive the wrapper.</param>
        /// <param name="center">The transform about which multi-handed grabs pivot and scale.</param>
        /// <param name="tryGetOriginPose">Resolves a selecting interactor's finger-tip (origin) pose.</param>
        public MultiGrabPointableElement(IPointableElement forwardElement, IInteractableView interactable, Transform center, TryGetOriginPose tryGetOriginPose)
        {
            ForwardElement = forwardElement;
            ForwardElement.WhenPointerEventRaised += HandleForwardElementEvent;
            _interactableView = interactable;
            _interactableView.WhenStateChanged += HandleStateChanged;
            _center = center;
            _tryGetOriginPose = tryGetOriginPose;
        }

        /// <summary>
        /// Unsubscribes from the owning <see cref="IInteractableView"/> so this element can be collected.
        /// </summary>
        public void Dispose()
        {
            ForwardElement.WhenPointerEventRaised -= HandleForwardElementEvent;
            _interactableView.WhenStateChanged -= HandleStateChanged;
        }

        // Relays a downstream-originated Cancel (e.g. Grabbable.TransferOnSecondSelection releasing the
        // prior grab when a new one selects) back up to the interactor so it stops selecting. Only acts on
        // a Cancel whose id is still tracked here: MG-originated release cancels (HandleRelease,
        // HandleStateChanged) remove the grab before forwarding, so their echoes are naturally ignored and
        // there is no loop. Removing the id before re-raising also guarantees no re-entrancy.
        private void HandleForwardElementEvent(PointerEvent evt)
        {
            if (evt.Type != PointerEventType.Cancel)
            {
                return;
            }

            int index = IndexOf(evt.Identifier);
            if (index < 0)
            {
                return;
            }

            _grabs.RemoveAt(index);
            _offset = ComputeOffset();
            WhenPointerEventRaised.Invoke(evt);
        }

        // When the owning interactable is disabled, cancel still-selecting grabs through the normal path so
        // the downstream element is released.
        private void HandleStateChanged(InteractableStateChangeArgs args)
        {
            if (args.NewState != InteractableState.Disabled)
            {
                return;
            }

            while (_grabs.Count > 0)
            {
                Grab grab = _grabs[_grabs.Count - 1];
                ProcessPointerEvent(new PointerEvent(grab.Id, PointerEventType.Cancel, grab.Forwarded, grab.Data));
            }

            _offset = Vector3.zero;
        }

        /// <summary>
        /// Implementation of <see cref="IPointableElement.ProcessPointerEvent(PointerEvent)"/>.
        /// </summary>
        public void ProcessPointerEvent(PointerEvent evt)
        {
            switch (evt.Type)
            {
                case PointerEventType.Hover:
                case PointerEventType.Unhover:
                    Forward(evt);
                    break;
                case PointerEventType.Move:
                    HandleMove(evt);
                    break;
                case PointerEventType.Select:
                    HandleSelect(evt);
                    break;
                case PointerEventType.Unselect:
                case PointerEventType.Cancel:
                    HandleRelease(evt);
                    break;
            }

            WhenPointerEventRaised.Invoke(evt);
        }

        private void HandleMove(PointerEvent evt)
        {
            int index = IndexOf(evt.Identifier);
            if (index < 0)
            {
                Forward(evt);
                return;
            }

            Grab grab = _grabs[index];
            grab.Provider = evt.Pose;
            grab.Data = evt.Data;
            _grabs[index] = grab;

            // A lone grab passes the provider pose straight through; two or more drive the downstream
            // element from the offset-anchored finger-tip so the pivot/scale stay put for any provider.
            Pose forwarded = _grabs.Count >= 2 ? Remap(HandPose(grab)) : evt.Pose;
            ForwardRecord(index, PointerEventType.Move, forwarded, evt.Data);
        }

        private void HandleSelect(PointerEvent evt)
        {
            _grabs.Add(new Grab(evt.Identifier, evt.Pose, evt.Data));

            if (_grabs.Count == 1)
            {
                // Pass-through: the provider pose drives the downstream element directly.
                _offset = Vector3.zero;
                ForwardRecord(0, PointerEventType.Select, evt.Pose, evt.Data);
                return;
            }

            _offset = ComputeOffset();

            // Forward the new grab's Select before rebasing so the downstream selecting count never drops
            // below the prior count during the rebase cycles (1 -> 2 -> 1 -> 2, not 1 -> 0 -> 1 -> 2).
            // This forward can synchronously drive a downstream transfer (Grabbable.TransferOnSecondSelection),
            // which cancels a prior grab back through HandleForwardElementEvent and mutates _grabs, so re-check
            // state afterward instead of trusting the pre-forward index.
            int newIndex = _grabs.Count - 1;
            ForwardRecord(newIndex, PointerEventType.Select, Remap(HandPose(_grabs[newIndex])), evt.Data);

            if (_grabs.Count <= 1)
            {
                // A downstream transfer released the prior grab, collapsing back to a single grab. The
                // survivor was forwarded at its remapped (offset-anchored) pose, but single-grab moves pass
                // the raw provider pose straight through; re-anchor it at that raw pose (as the release path
                // does) so the first post-transfer move does not jump the object by the old offset.
                _offset = Vector3.zero;
                if (_grabs.Count == 1)
                {
                    Rebase(0, _grabs[0].Provider);
                }
                return;
            }

            newIndex = IndexOf(evt.Identifier);
            for (int i = 0; i < _grabs.Count; i++)
            {
                if (i == newIndex)
                {
                    continue;
                }
                Rebase(i, Remap(HandPose(_grabs[i])));
            }
        }

        private void HandleRelease(PointerEvent evt)
        {
            int index = IndexOf(evt.Identifier);
            if (index < 0)
            {
                Forward(evt);
                return;
            }

            bool releasingToSingleGrab = _grabs.Count == 2;
            Grab released = _grabs[index];
            _grabs.RemoveAt(index);

            _offset = ComputeOffset();

            if (releasingToSingleGrab)
            {
                // Release first so the downstream drops to one-grab mode, then reselect the survivor in the
                // provider coordinate space that later single-grab moves use, so there is no jump.
                Forward(released.Id, evt.Type, released.Forwarded, evt.Data);
                Rebase(0, _grabs[0].Provider);
                return;
            }

            // Re-base survivors before forwarding the release so the downstream selecting count never dips
            // below the post-release count (3 -> 2 -> 3 -> 2, not 3 -> 2 -> 1 -> 2).
            for (int i = 0; i < _grabs.Count; i++)
            {
                Rebase(i, Remap(HandPose(_grabs[i])));
            }

            Forward(released.Id, evt.Type, released.Forwarded, evt.Data);
        }

        // Moves a grab's downstream registration to reselectPose with no object jump: a no-op unselect at
        // the current forwarded pose followed by a re-select so the transformer re-captures.
        private void Rebase(int index, Pose reselectPose)
        {
            Grab grab = _grabs[index];
            Forward(grab.Id, PointerEventType.Unselect, grab.Forwarded, grab.Data);
            Forward(grab.Id, PointerEventType.Select, reselectPose, grab.Data);
            grab.Forwarded = reselectPose;
            _grabs[index] = grab;
        }

        // A grab's finger-tip pose, used for the centroid/remap math: the selecting interactor's origin
        // pose (from the injected delegate) when it resolves, otherwise the grab's provider pose.
        private Pose HandPose(Grab grab) => SourcePose(grab.Id, grab.Provider);

        // The selecting interactor's origin pose (from the injected delegate) when it resolves, otherwise
        // the supplied fallback (the incoming pose).
        private Pose SourcePose(int id, Pose fallback)
        {
            foreach (IInteractorView interactor in _interactableView.SelectingInteractorViews)
            {
                if (interactor.Identifier == id && _tryGetOriginPose(interactor, out Pose originPose))
                {
                    return originPose;
                }
            }
            return fallback;
        }

        private int IndexOf(int id)
        {
            for (int i = 0; i < _grabs.Count; i++)
            {
                if (_grabs[i].Id == id)
                {
                    return i;
                }
            }
            return -1;
        }

        private Vector3 ComputeOffset()
        {
            return _grabs.Count >= 2 ? Center() - Centroid() : Vector3.zero;
        }

        private Vector3 Center()
        {
            return _center.position;
        }

        private Vector3 Centroid()
        {
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < _grabs.Count; i++)
            {
                sum += HandPose(_grabs[i]).position;
            }
            return sum / _grabs.Count;
        }

        private Pose Remap(Pose hand) => new Pose(hand.position + _offset, hand.rotation);

        private void Forward(PointerEvent evt)
        {
            ForwardElement?.ProcessPointerEvent(evt);
        }

        private void Forward(int identifier, PointerEventType type, Pose pose, object data = null)
        {
            ForwardElement?.ProcessPointerEvent(new PointerEvent(identifier, type, pose, data));
        }

        // Forwards an event for a grab and records the forwarded pose for later no-op re-bases.
        private void ForwardRecord(int index, PointerEventType type, Pose pose, object data)
        {
            Grab grab = _grabs[index];
            grab.Forwarded = pose;
            _grabs[index] = grab;
            Forward(grab.Id, type, pose, data);
        }
    }
}
