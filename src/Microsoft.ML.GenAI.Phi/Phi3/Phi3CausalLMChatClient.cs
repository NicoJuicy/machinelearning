﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;
using Microsoft.ML.GenAI.Core;
using Microsoft.ML.Tokenizers;

namespace Microsoft.ML.GenAI.Phi;

public class Phi3CausalLMChatClient : CausalLMPipelineChatClient<Tokenizer, Phi3ForCausalLM>
{
    private readonly string _eotToken = "<|end|>";

    public Phi3CausalLMChatClient(
        ICausalLMPipeline<Tokenizer, Phi3ForCausalLM> pipeline,
        IMEAIChatTemplateBuilder? templateBuilder = null,
        ChatClientMetadata? metadata = null)
        : base(
            pipeline,
            templateBuilder ?? Phi3ChatTemplateBuilder.Instance,
            metadata ?? new ChatClientMetadata(defaultModelId: nameof(Phi3CausalLMChatClient)))
    {
    }

    public override Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new ChatOptions();

        if (options.StopSequences != null)
        {
            options.StopSequences.Add(_eotToken);
        }
        else
        {
            options.StopSequences = [_eotToken];
        }

        return base.GetResponseAsync(messages, options, cancellationToken);
    }

    public override IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new ChatOptions();
        options.StopSequences ??= [];
        options.StopSequences.Add(_eotToken);

        return base.GetStreamingResponseAsync(messages, options, cancellationToken);
    }
}
