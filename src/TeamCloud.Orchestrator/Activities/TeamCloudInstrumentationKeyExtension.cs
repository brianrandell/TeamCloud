﻿/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System.Threading.Tasks;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using TeamCloud.Orchestration;

namespace TeamCloud.Orchestrator.Activities
{
    internal static class TeamCloudInstrumentationKeyExtension
    {
        public static Task<string> GetInstrumentationKeyAsync(this IDurableOrchestrationContext functionContext)
            => functionContext.CallActivityWithRetryAsync<string>(nameof(TeamCloudInstrumentationKeyActivity), null);
    }
}
