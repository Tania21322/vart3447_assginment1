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

using Oculus.Interaction;
using System;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Attach to a gameobject to have it's transform updated to match a source transform. If the
/// configured interactor starts selecting, the blend target transform is applied to the source
/// transform following an Animation Curve.
/// </summary>
[Experimental]
public class SelectBlendedTransform : MonoBehaviour, ITimeConsumer
{
    [Tooltip("The base source transform.")]
    [SerializeField]
    private Transform _sourceTransform;
    public Transform SourceTransform { get => _sourceTransform; set => _sourceTransform = value; }

    [Tooltip("Blend Target Transform is blended into the source transform depending on select "
        + "duration.")]
    [SerializeField]
    private Transform _blendTargetTransform;
    public Transform BlendTargetTransform { get => _blendTargetTransform; set => _blendTargetTransform = value; }

    [Tooltip("If this interactor is selecting,"
        + " the blend target will start to be blended with the source transform.")]
    [SerializeField, Interface(typeof(IInteractor))]
    private Object _interactor;
    private IInteractor Interactor = null;

    [Tooltip("Define how quickly the blend target should be combined with the source transform pose.")]
    [SerializeField]
    private AnimationCurve _blendCurve = AnimationCurve.EaseInOut(0.1f, 0, 1, 1);
    public AnimationCurve BlendCurve { get => _blendCurve; set => _blendCurve = value; }

    [Tooltip("If true, the blend target transform rotation will be blended with the source transform.")]
    [SerializeField]
    private bool _blendRotation = true;
    public bool BlendRotation { get => _blendRotation; set => _blendRotation = value; }

    [Tooltip("If true, the blend target transform position will be blended with the source transform.")]
    [SerializeField]
    private bool _blendPosition = true;
    public bool BlendPosition { get => _blendPosition; set => _blendPosition = value; }

    private static readonly Func<float> defaultTimeProvider = () => Time.time;
    private Func<float> _timeProvider = defaultTimeProvider;
    public void SetTimeProvider(Func<float> timeProvider)
    {
        if (timeProvider == null)
        {
            timeProvider = defaultTimeProvider;
        }
        _timeProvider = timeProvider;
    }

    private bool _selectActive = false;
    private float _selectStartedAt = 0;
    private bool _blendStart = false;
    private Pose _blendOrigin = Pose.identity;
    private Pose _blendToSourceTransform = Pose.identity;


    protected void Awake()
    {
        if (Interactor == null)
        {
            Interactor = _interactor as IInteractor;
        }
    }

    public void OnEnable()
    {
        Interactor.WhenStateChanged += InteractorOnWhenStateChanged;
    }

    public void OnDisable()
    {
        Interactor.WhenStateChanged -= InteractorOnWhenStateChanged;
    }

    private void InteractorOnWhenStateChanged(InteractorStateChangeArgs obj)
    {
        _selectActive = obj.NewState == InteractorState.Select;
        if (!_selectActive)
        {
            _blendStart = false;
        }
        _selectStartedAt = _timeProvider.Invoke();
    }

    public void Update()
    {
        var blendPosition = (!_selectActive) ? 0 : _blendCurve.Evaluate(_timeProvider.Invoke() - _selectStartedAt);

        if (blendPosition <= 0)
        {
            transform.SetPositionAndRotation(_sourceTransform.position, _sourceTransform.rotation);
            return;
        }

        var sourcePose = _sourceTransform.GetPose();
        var blendPose = _blendTargetTransform.GetPose();

        var sourceBlendPosDelta = sourcePose.position - blendPose.position;

        var targetPose = blendPose;
        targetPose.position += sourceBlendPosDelta;

        if (!_blendStart)
        {
            _blendStart = true;
            _blendOrigin = blendPose;
            _blendToSourceTransform = PoseUtils.Delta(targetPose, sourcePose);
        }

        targetPose.Premultiply(_blendToSourceTransform);
        if (_blendPosition)
        {
            var blendOriginDelta = blendPose.position - _blendOrigin.position;
            targetPose.position += blendOriginDelta;
        }

        Pose destPose = sourcePose;
        destPose.Lerp(targetPose, blendPosition);

        if (!_blendPosition)
        {
            destPose.position = _sourceTransform.position;
        }
        if (!_blendRotation)
        {
            destPose.rotation = _sourceTransform.rotation;
        }

        transform.SetPose(destPose);
    }

    #region Inject

    public void InjectAllSelectBlendedTransform(IInteractor interactor)
    {
        InjectInteractor(interactor);
    }

    public void InjectInteractor(IInteractor interactor)
    {
        _interactor = interactor as Object;
        Interactor = interactor;
    }

    #endregion

}
