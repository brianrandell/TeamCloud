/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using FluentValidation;
using TeamCloud.Model.Data;

namespace TeamCloud.Model.Validation.Data
{
    public sealed class AzureIdentityValidator : AbstractValidator<ProjectIdentity>
    {
        public AzureIdentityValidator()
        {
            RuleFor(obj => obj.Id).MustBeGuid();
            RuleFor(obj => obj.ApplicationId).NotEmpty();
            RuleFor(obj => obj.Secret).NotEmpty();
        }
    }
}
