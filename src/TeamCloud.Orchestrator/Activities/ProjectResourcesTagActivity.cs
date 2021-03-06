﻿/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Extensions.Logging;
using TeamCloud.Azure.Resources;
using TeamCloud.Model.Data;

namespace TeamCloud.Orchestrator.Activities
{
    public class ProjectResourcesTagActivity
    {
        private readonly IAzureResourceService azureResourceService;

        public ProjectResourcesTagActivity(IAzureResourceService azureResourceService)
        {
            this.azureResourceService = azureResourceService ?? throw new ArgumentNullException(nameof(azureResourceService));
        }

        [FunctionName(nameof(ProjectResourcesTagActivity))]
        public async Task RunActivity(
            [ActivityTrigger] Project project,
            ILogger log)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            if (!string.IsNullOrEmpty(project.ResourceGroup?.ResourceGroupId))
            {
                var resourceGroup = await azureResourceService
                    .GetResourceGroupAsync(project.ResourceGroup.SubscriptionId, project.ResourceGroup.ResourceGroupName)
                    .ConfigureAwait(false);

                if (resourceGroup is null)
                {
                    log.LogWarning($"Could not find resource group '{project.ResourceGroup.ResourceGroupName}' in subscription '{project.ResourceGroup.SubscriptionId}' for tagging.");
                }
                else
                {
                    await resourceGroup
                        .SetTagsAsync(project.Tags)
                        .ConfigureAwait(false);
                }
            }
        }
    }
}
