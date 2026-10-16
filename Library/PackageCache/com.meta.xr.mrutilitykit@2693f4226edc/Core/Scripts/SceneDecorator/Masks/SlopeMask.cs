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
using Meta.XR.Util;
using UnityEngine;

namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    /// <summary>
    /// A mask that samples the slope of a candidate.
    /// </summary>
    [Feature(Feature.Scene)]
    [Obsolete("SceneDecorator is deprecated and will be removed in a future version.")]
    public class SlopeMask : Mask
    {
        /// <summary>
        /// Returns the slope of the candidate's surface
        /// </summary>
        /// <param name="c">Candidate containing slope information</param>
        /// <returns>The slope value of the surface</returns>
        public override float SampleMask(Candidate c)
        {
            return c.slope;
        }

        /// <summary>
        /// This check always returns true for slope mask
        /// </summary>
        /// <param name="c">Candidate with the information from the distribution</param>
        /// <returns>Always returns true</returns>
        public override bool Check(Candidate c)
        {
            return true;
        }
    }
}
