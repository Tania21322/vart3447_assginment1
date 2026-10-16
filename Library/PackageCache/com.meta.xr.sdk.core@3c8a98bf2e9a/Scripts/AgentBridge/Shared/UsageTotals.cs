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

#nullable enable

using System;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Cumulative AI token/cost usage for a conversation, as reported by the active
    /// provider. Providers that don't report usage leave these at zero.
    /// </summary>
    [Serializable]
    public class UsageTotals
    {
        /// <summary>Prompt (input) tokens consumed.</summary>
        public long InputTokens;

        /// <summary>Completion (output) tokens produced.</summary>
        public long OutputTokens;

        /// <summary>Input tokens served from the provider's prompt cache, when reported.</summary>
        public long CacheReadTokens;

        /// <summary>Input tokens written to the provider's prompt cache, when reported.</summary>
        public long CacheCreationTokens;

        /// <summary>Estimated cost in USD, when the provider reports it.</summary>
        public double CostUsd;

        /// <summary>Headline token count (input + output).</summary>
        public long TotalTokens => InputTokens + OutputTokens;
    }
}
