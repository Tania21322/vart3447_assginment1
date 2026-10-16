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

/************************************************************************************
 * Filename    :   MetaXRAudioRoomAcousticProperties.cs
 * Content     :   Interface into the Meta XR Audio shoebox reflections system
 ***********************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using static MetaXRAudioRoomAcousticProperties;

/// \brief Class that provides easy control over the shoebox room simulation.
///
/// One of these classes should be present in any scene you wish the control the parameters of shoebox reverberation. Multiple instantiations of this class within one scene will result in undefined behavior.
//
/// \see MetaXRAudioNativeInterface
public sealed class MetaXRAudioRoomAcousticProperties : MonoBehaviour
{
    /// \brief Whether to center the room model on the listener or position its center in world space.
    ///
    /// If set to false, the room model's center will be placed at the transform position.
    [Tooltip("Center the room model on the listener. When disabled, center the room model on the GameObject this script is attached to.")]
    public bool lockPositionToListener = true;

    /// \brief Width of the room, in meters.
    [Tooltip("Width of the room model in meters")]
    public float width = 8.0f;

    /// \brief Height of the room, in meters.
    [Tooltip("Height of the room model in meters")]
    public float height = 3.0f;

    /// \brief Depth of the room, in meters.
    [Tooltip("Depth of the room model in meters")]
    public float depth = 5.0f;

    /// \brief Material preset of the left wall of the room model.
    /// \see MaterialPreset
    [Tooltip("Material of the left wall of the room model")]
    public MaterialPreset leftMaterial = MaterialPreset.GypsumBoard;

    /// \brief Material preset of the right wall of the room model.
    /// \see MaterialPreset
    [Tooltip("Material of the right wall of the room model")]
    public MaterialPreset rightMaterial = MaterialPreset.GypsumBoard;

    /// \brief Material preset of the ceiling of the room model.
    /// \see MaterialPreset
    [Tooltip("Material of the ceiling of the room model")]
    public MaterialPreset ceilingMaterial = MaterialPreset.AcousticTile;

    /// \brief Material preset of the floor of the room model.
    /// \see MaterialPreset
    [Tooltip("Material of the floor of the room model")]
    public MaterialPreset floorMaterial = MaterialPreset.Carpet;

    /// \brief Material preset of the front wall of the room model.
    /// \see MaterialPreset
    [Tooltip("Material of the front wall of the room model")]
    public MaterialPreset frontMaterial = MaterialPreset.GypsumBoard;

    /// \brief Material preset of the back wall of the room model.
    /// \see MaterialPreset
    [Tooltip("Material of the back wall of the room model")]
    public MaterialPreset backMaterial = MaterialPreset.GypsumBoard;

    /// \brief Current room clutter factor.
    ///
    /// This is a simplified interface to the native, frequency-dependent clutter factor. The singular value represented here is transformed into a the frequency-dependent clutter factor of MetaXRAudioNativeInterface#NativeInterface#SetRoomClutterFactor through means of the following formula:
    ///
    /// \code
    /// factor = clutterFactor;
    /// for (int band = kAudioBandCount - 1; band >= 0; --band)
    /// {
    ///     clutterFactorBands[band] = factor;
    ///     factor *= 0.5f; // clutter has less impact on low frequencies
    /// }
    /// \endcode
    /// \see MetaXRAudioNativeInterface#NativeInterface#SetRoomClutterFactor
    [Tooltip("Diffuses the reflections and reverberation to simulate objects inside the room. Zero represents a completely empty room.")]
    [Range(0.0f, 1.0f)]
    public float clutterFactor = 0.5f;

    private const int kAudioBandCount = 4;
    private float[] clutterFactorBands = new float[kAudioBandCount];

    /// \brief Array of 6 materials, each with 4 frequency dependent reflection coefficients in [Left, Right, Ceiling, Floor, Front, Back] order.
    ///
    /// \see MetaXRAudioNativeInterface#NativeInterface#SetAdvancedBoxRoomParameters
    float[] wallMaterials = new float[6 * kAudioBandCount];

    /// \brief Enum representing material presets.
    ///
    /// \see MetaXRAudioRoomAcousticProperties#SetWallMaterialPreset
    public enum MaterialPreset : int
    {
        AcousticTile = 0,
        Brick,
        BrickPainted,
        Cardboard,
        Carpet,
        CarpetHeavy,
        CarpetHeavyPadded,
        CeramicTile,
        Concrete,
        ConcreteRough,
        ConcreteBlock,
        ConcreteBlockPainted,
        Curtain,
        Foliage,
        Glass,
        GlassHeavy,
        Grass,
        Gravel,
        GypsumBoard,
        Marble,
        Mud,
        PlasterOnBrick,
        PlasterOnConcreteBlock,
        Rubber,
        Soil,
        SoundProof,
        Snow,
        Steel,
        Stone,
        Vent,
        Water,
        WoodThin,
        WoodThick,
        WoodFloor,
        WoodOnConcrete,
        MetaDefault
    }

    [RuntimeInitializeOnLoadMethod]
    static void CheckSceneHasRoom()
    {
#pragma warning disable CS0618 // Type or member is obsolete
        MetaXRAudioRoomAcousticProperties[] rooms = FindObjectsOfType<MetaXRAudioRoomAcousticProperties>();
#pragma warning restore CS0618 // Type or member is obsolete
        if (rooms.Length == 0)
        {
            Debug.Log("No Meta XR Audio Room found, setting default room");
            GameObject temp = new GameObject("Temporary Room");
            MetaXRAudioRoomAcousticProperties tempRoom = temp.AddComponent<MetaXRAudioRoomAcousticProperties>();
            tempRoom.Update();
            DestroyImmediate(temp);
        }

        if (rooms.Length > 1)
        {
            Debug.LogError("Multiple Meta XR Audio Rooms found, only one is allowed!");
        }
    }

    void Update()
    {
        SetWallMaterialPreset(0, rightMaterial);
        SetWallMaterialPreset(1, leftMaterial);
        SetWallMaterialPreset(2, ceilingMaterial);
        SetWallMaterialPreset(3, floorMaterial);
        SetWallMaterialPreset(4, frontMaterial);
        SetWallMaterialPreset(5, backMaterial);

        MetaXRAudioNativeInterface.Interface.SetAdvancedBoxRoomParameters(width, height, depth, lockPositionToListener,
            transform.position, wallMaterials);
        float factor = clutterFactor;
        for (int band = kAudioBandCount - 1; band >= 0; --band)
        {
            clutterFactorBands[band] = factor;
            factor *= 0.5f; // clutter has less impact on low frequencies
        }
        MetaXRAudioNativeInterface.Interface.SetRoomClutterFactor(clutterFactorBands);
    }

    public static float[] GetMaterialPresetBands(MaterialPreset preset)
    {
        switch (preset)
        {
            case MaterialPreset.AcousticTile: return new float[] { 0.488168418f, 0.361475229f, 0.339595377f, 0.498946249f };
            case MaterialPreset.Brick: return new float[] { 0.975468814f, 0.972064495f, 0.949180186f, 0.930105388f };
            case MaterialPreset.BrickPainted: return new float[] { 0.975710571f, 0.983324170f, 0.978116691f, 0.970052719f };
            case MaterialPreset.Cardboard: return new float[] { 0.590000f, 0.435728f, 0.251650f, 0.208000f };
            case MaterialPreset.Carpet: return new float[] { 0.987633705f, 0.905486643f, 0.583110571f, 0.351053834f };
            case MaterialPreset.CarpetHeavy: return new float[] { 0.977633715f, 0.859082878f, 0.526479602f, 0.370790422f };
            case MaterialPreset.CarpetHeavyPadded: return new float[] { 0.910534739f, 0.530433178f, 0.294055820f, 0.270105422f };
            case MaterialPreset.CeramicTile: return new float[] { 0.990000010f, 0.990000010f, 0.982753932f, 0.980000019f };
            case MaterialPreset.Concrete: return new float[] { 0.990000010f, 0.983324170f, 0.980000019f, 0.980000019f };
            case MaterialPreset.ConcreteRough: return new float[] { 0.989408433f, 0.964494646f, 0.922127008f, 0.900105357f };
            case MaterialPreset.ConcreteBlock: return new float[] { 0.635267377f, 0.652230680f, 0.671053469f, 0.789051592f };
            case MaterialPreset.ConcreteBlockPainted: return new float[] { 0.902957916f, 0.940235913f, 0.917584062f, 0.919947326f };
            case MaterialPreset.Curtain: return new float[] { 0.686494231f, 0.545859993f, 0.310078561f, 0.399473131f };
            case MaterialPreset.Foliage: return new float[] { 0.518259346f, 0.503568292f, 0.578688800f, 0.690210819f };
            case MaterialPreset.Glass: return new float[] { 0.655915797f, 0.800631821f, 0.918839693f, 0.923488140f };
            case MaterialPreset.GlassHeavy: return new float[] { 0.827098966f, 0.950222731f, 0.974604130f, 0.980000019f };
            case MaterialPreset.Grass: return new float[] { 0.881126285f, 0.507170796f, 0.131893098f, 0.0103688836f };
            case MaterialPreset.Gravel: return new float[] { 0.729294717f, 0.373122454f, 0.255317450f, 0.200263441f };
            case MaterialPreset.GypsumBoard: return new float[] { 0.721240044f, 0.927690148f, 0.934302270f, 0.910105407f };
            case MaterialPreset.Marble: return new float[] { 0.990000f, 0.990000f, 0.982754f, 0.980000f };
            case MaterialPreset.Mud: return new float[] { 0.844084f, 0.726577f, 0.794683f, 0.849737f };
            case MaterialPreset.PlasterOnBrick: return new float[] { 0.975696504f, 0.979106009f, 0.961063504f, 0.950052679f };
            case MaterialPreset.PlasterOnConcreteBlock: return new float[] { 0.881774724f, 0.924773932f, 0.951497555f, 0.959947288f };
            case MaterialPreset.Rubber: return new float[] { 0.950000f, 0.916621f, 0.936230f, 0.950000f };
            case MaterialPreset.Soil: return new float[] { 0.844084203f, 0.634624243f, 0.416662872f, 0.400000036f };
            case MaterialPreset.SoundProof: return new float[] { 0.000000000f, 0.000000000f, 0.000000000f, 0.000000000f };
            case MaterialPreset.Snow: return new float[] { 0.532252669f, 0.154535770f, 0.0509644151f, 0.0500000119f };
            case MaterialPreset.Steel: return new float[] { 0.793111682f, 0.840140402f, 0.925591767f, 0.979736567f };
            case MaterialPreset.Stone: return new float[] { 0.980000f, 0.978740f, 0.955701f, 0.950000f };
            case MaterialPreset.Vent: return new float[] { 0.847042f, 0.620450f, 0.702170f, 0.799473f };
            case MaterialPreset.Water: return new float[] { 0.970588267f, 0.971753478f, 0.978309572f, 0.970052719f };
            case MaterialPreset.WoodThin: return new float[] { 0.592423141f, 0.858273327f, 0.917242289f, 0.939999998f };
            case MaterialPreset.WoodThick: return new float[] { 0.812957883f, 0.895329595f, 0.941304684f, 0.949947298f };
            case MaterialPreset.WoodFloor: return new float[] { 0.852366328f, 0.898992121f, 0.934784114f, 0.930052698f };
            case MaterialPreset.WoodOnConcrete: return new float[] { 0.959999979f, 0.941232264f, 0.937923789f, 0.930052698f };
            case MaterialPreset.MetaDefault: return new float[] { 0.9f, 0.9f, 0.9f, 0.9f };
            default: return new float[] { 0.9f, 0.9f, 0.9f, 0.9f };
        }
    }

    void SetWallMaterialPreset(int wallIndex, MaterialPreset materialPreset)
    {
        float[] bands = GetMaterialPresetBands(materialPreset);
        wallMaterials[wallIndex * 4 + 0] = bands[0];
        wallMaterials[wallIndex * 4 + 1] = bands[1];
        wallMaterials[wallIndex * 4 + 2] = bands[2];
        wallMaterials[wallIndex * 4 + 3] = bands[3];
    }


#if UNITY_EDITOR
    private Component _AudioListener = null;

    private Component AudioListener
    {
        get
        {
            if (_AudioListener == null)
            {
                _AudioListener = FindFMODListener();
                // null fmod listener, find unity listener...
                if (_AudioListener == null)
                    _AudioListener = FindFirstObjectByType<AudioListener>();
            }
            return _AudioListener;

            // BFS find the FMOD studio listener without directly using type.
            // We could be using unity native.
            Component FindFMODListener()
            {
                Component FMODListener = null;
                GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
                // BFS because camera is unlikely to be deeply nested...
                Queue<GameObject> gameObjectQueue = new Queue<GameObject>(rootObjects);
                while (gameObjectQueue.Count > 0)
                {
                    GameObject go = gameObjectQueue.Dequeue();
                    const string FMODListenerTypeName = "StudioListener";
                    FMODListener = go.GetComponent(FMODListenerTypeName);
                    if (FMODListener != null)
                        break;

                    foreach(Transform child in go.transform)
                        gameObjectQueue.Enqueue(child.gameObject);
                }

                return FMODListener;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 gizmoPosition = transform.position;
        if (lockPositionToListener)
        {
            Component ListenerComponent = AudioListener;
            if(ListenerComponent == null)
            {
                Debug.LogWarning("[MetaXRAudioRoomAcousticProperties.OnDrawGizmosSelected] Attempting to draw gizmo with lockPositionToListener = true " +
                    "but we did not find an Audio Listener (Unity native or FMOD) to draw gizmo from. IF using FMOD: attach StudioListener to gameobject. " +
                    "IF using unity audio with meta audio sdk, attach unity AudioListener component to gameobject.");
                return;
            }

            gizmoPosition = ListenerComponent.transform.position;
        }

        Gizmos.color = new Color(0.8f, 0.8f, 0.8f, 0.25f);
        Gizmos.DrawCube(gizmoPosition, new Vector3(width, height, depth));
    }
#endif
}
