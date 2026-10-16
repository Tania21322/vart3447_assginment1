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
using Meta.XR.Json;

namespace Meta.XR.AI.AgentBridge.Acp
{
    /// <summary>
    /// Parser for ACP (Agent Communication Protocol) session updates.
    /// Converts ACP streaming updates to the shared ConversationInfo model.
    /// Used by Gemini CLI service to map ACP events to conversation messages.
    /// </summary>
    internal static class AcpParser
    {
        /// <summary>
        /// Converts an ACP session/update notification into a ConversationInfo.
        /// Used by GeminiCliService to map ACP streaming updates to the shared conversation model.
        /// </summary>
        /// <param name="update">The session update notification from ACP</param>
        /// <returns>Conversation info with content and message type, or null if the update should be skipped</returns>
        internal static ConversationStreamParser.ConversationInfo? GetConversationContentFromAcp(SessionUpdateParams update)
        {
            try
            {
                var updateType = update.UpdateType;
                if (string.IsNullOrEmpty(updateType))
                {
                    return null;
                }

                switch (updateType)
                {
                    case "agent_message_chunk":
                    {
                        var text = ExtractTextFromAcpContent(update.Update["content"]);
                        if (string.IsNullOrEmpty(text)) return null;

                        return new ConversationStreamParser.ConversationInfo
                        {
                            HasContent = true,
                            MessageType = "assistant",
                            Content = text!,
                            MessageId = $"acp_{update.SessionId}_msg",
                            IsStreaming = true,
                            IsDelta = true
                        };
                    }

                    case "agent_thought_chunk":
                    {
                        var text = ExtractTextFromAcpContent(update.Update["content"]);
                        if (string.IsNullOrEmpty(text)) return null;

                        return new ConversationStreamParser.ConversationInfo
                        {
                            HasContent = true,
                            MessageType = "thinking",
                            Content = text!,
                            MessageId = $"acp_{update.SessionId}_thought",
                            IsStreaming = true,
                            IsDelta = true
                        };
                    }

                    case "tool_call":
                    {
                        var toolCallId = update.Update["toolCallId"]?.ToString() ?? "";
                        var title = update.Update["title"]?.ToString() ?? "Tool call";
                        var kind = update.Update["kind"]?.ToString();

                        // Map the ACP tool "kind" to a label the HRT status line renders nicely:
                        // FormatAIToolActivity turns "[Read] (foo.cs)" into "Reading foo.cs" and shows
                        // the parenthesized title verbatim for unmapped kinds.
                        var toolLabel = kind switch
                        {
                            "read" => "Read",
                            "edit" => "Edit",
                            "execute" => "Bash",
                            "search" => "Grep",
                            _ => string.IsNullOrEmpty(kind) ? "Tool" : kind
                        };

                        return new ConversationStreamParser.ConversationInfo
                        {
                            HasContent = true,
                            MessageType = "tool_use",
                            Content = $"[{toolLabel}] ({title})",
                            ToolName = toolLabel,
                            MessageId = $"acp_tool_{toolCallId}",
                            IsStreaming = false,
                            IsDelta = false
                        };
                    }

                    case "tool_call_update":
                    {
                        var toolCallId = update.Update["toolCallId"]?.ToString() ?? "";
                        var status = update.Update["status"]?.ToString();
                        var contentArr = update.Update["content"] as JsonArray;

                        // Extract text from content blocks if present
                        var resultText = "";
                        if (contentArr != null)
                        {
                            foreach (var block in contentArr)
                            {
                                var blockType = block?["type"]?.ToString();
                                if (blockType == "text")
                                {
                                    resultText += block?["text"]?.ToString() ?? "";
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(resultText) && !string.IsNullOrEmpty(status))
                        {
                            resultText = status;
                        }

                        if (string.IsNullOrEmpty(resultText))
                        {
                            return null;
                        }

                        return new ConversationStreamParser.ConversationInfo
                        {
                            HasContent = true,
                            MessageType = "tool_result",
                            Content = resultText!,
                            MessageId = $"acp_tool_result_{toolCallId}",
                            IsStreaming = false,
                            IsDelta = false
                        };
                    }

                    case "plan":
                    {
                        var text = ExtractTextFromAcpContent(update.Update["content"]);
                        if (string.IsNullOrEmpty(text)) return null;

                        return new ConversationStreamParser.ConversationInfo
                        {
                            HasContent = true,
                            MessageType = "assistant",
                            Content = $"**Plan:**\n{text}",
                            MessageId = $"acp_{update.SessionId}_plan",
                            IsStreaming = false,
                            IsDelta = false
                        };
                    }

                    case "user_message_chunk":
                    {
                        // Process user messages from server - server is source of truth
                        // The ConversationManager will deduplicate if the client already added locally
                        var text = ExtractTextFromAcpContent(update.Update["content"]);
                        if (string.IsNullOrEmpty(text)) return null;

                        return new ConversationStreamParser.ConversationInfo
                        {
                            HasContent = true,
                            MessageType = "user",
                            Content = text!,
                            MessageId = $"acp_{update.SessionId}_user",
                            IsStreaming = false,
                            IsDelta = false
                        };
                    }

                    case "usage_update":
                    {
                        // Surface usage (no conversation message) so the service can
                        // accumulate it per caller. ACP reports cumulative input/output
                        // tokens for the turn.
                        var inputTokens = update.Update["inputTokens"]?.ToObject<int?>() ?? 0;
                        var outputTokens = update.Update["outputTokens"]?.ToObject<int?>() ?? 0;
                        Log.Info($"ACP usage: input={inputTokens}, output={outputTokens}");
                        if (inputTokens == 0 && outputTokens == 0)
                        {
                            return null;
                        }
                        return new ConversationStreamParser.ConversationInfo
                        {
                            HasContent = false,
                            Usage = new UsageTotals
                            {
                                InputTokens = inputTokens,
                                OutputTokens = outputTokens
                            }
                        };
                    }

                    default:
                        Log.Info($"ACP: Unhandled session update type: {updateType}");
                        return null;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Error parsing ACP session update: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Extract text from an ACP content block (which may be a JsonObject with type/text or a plain string).
        /// </summary>
        private static string? ExtractTextFromAcpContent(JsonNode? content)
        {
            if (content == null) return null;

            // Content can be a single content block object
            if (content is JsonObject obj)
            {
                var type = obj["type"]?.ToString();
                if (type == "text")
                {
                    return obj["text"]?.ToString();
                }
                // For other types, stringify the whole thing
                return obj.ToString();
            }

            // Or it can be a string directly
            if (content.Type == JsonNodeType.String)
            {
                return content.ToString();
            }

            // Or it can be an array of content blocks
            if (content is JsonArray arr)
            {
                var sb = new System.Text.StringBuilder();
                foreach (var block in arr)
                {
                    var blockType = block?["type"]?.ToString();
                    if (blockType == "text")
                    {
                        sb.Append(block?["text"]?.ToString() ?? "");
                    }
                }
                var result = sb.ToString();
                return string.IsNullOrEmpty(result) ? null : result;
            }

            return content.ToString();
        }
    }
}
