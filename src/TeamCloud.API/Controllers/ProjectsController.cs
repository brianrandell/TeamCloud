/**
 *  Copyright (c) Microsoft Corporation.
 *  Licensed under the MIT License.
 */

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamCloud.API.Data;
using TeamCloud.API.Services;
using TeamCloud.Data;
using TeamCloud.Model.Commands;
using TeamCloud.Model.Data;

namespace TeamCloud.API.Controllers
{
    [ApiController]
    [Route("api/projects")]
    [Authorize(Policy = "projectRead")]
    public class ProjectsController : ControllerBase
    {
        readonly UserService userService;
        readonly Orchestrator orchestrator;
        readonly IProjectsRepositoryReadOnly projectsRepository;

        public ProjectsController(UserService userService, Orchestrator orchestrator, IProjectsRepositoryReadOnly projectsRepository)
        {
            this.userService = userService ?? throw new ArgumentNullException(nameof(userService));
            this.orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
            this.projectsRepository = projectsRepository ?? throw new ArgumentNullException(nameof(projectsRepository));
        }

        private User CurrentUser => new User()
        {
            Id = userService.CurrentUserId,
            Role = UserRoles.Project.Owner
        };

        private async Task<List<User>> ResolveUsersAsync(ProjectDefinition projectDefinition)
        {
            var tasks = projectDefinition.Users.Select(user => userService.GetUserAsync(user));
            var users = await Task.WhenAll(tasks).ConfigureAwait(false);
            var owners = users.Where(user => user.Role.Equals(UserRoles.Project.Owner));

            return users
                .Where(user => user.Role.Equals(UserRoles.Project.Member))
                .Except(owners, new UserComparer()) // filter out owners
                .Union(owners) // union members and owners
                .ToList();
        }

        [HttpGet]
        public async IAsyncEnumerable<Project> Get()
        {
            var projects = projectsRepository
                .ListAsync();

            await foreach (var project in projects)
                yield return project;
        }

        [HttpGet("{projectId:guid}")]
        public async Task<IActionResult> Get(Guid projectId)
        {
            var project = await projectsRepository
                .GetAsync(projectId)
                .ConfigureAwait(false);

            if (project is null)
                return new NotFoundResult();

            return new OkObjectResult(project);
        }

        [HttpPost]
        [Authorize(Policy = "projectCreate")]
        public async Task<IActionResult> Post([FromBody] ProjectDefinition projectDefinition)
        {
            var users = await ResolveUsersAsync(projectDefinition)
                .ConfigureAwait(false);

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Users = users,
                Name = projectDefinition.Name,
                Tags = projectDefinition.Tags
            };

            var isExisting = await projectsRepository
                .NameExistsAsync(project)
                .ConfigureAwait(false);

            if (isExisting)
                return new ConflictObjectResult($"A Project with name '{project.Name}' already exists.  Project names must be unique.  Please try your request again with a unique name.");

            var command = new ProjectCreateCommand(CurrentUser, project);

            var commandResult = await orchestrator
                .InvokeAsync(command)
                .ConfigureAwait(false);

            return commandResult.ActionResult();
        }

        [HttpDelete("{projectId:guid}")]
        [Authorize(Policy = "projectDelete")]
        public async Task<IActionResult> Delete(Guid projectId)
        {
            var project = await projectsRepository
                .GetAsync(projectId)
                .ConfigureAwait(false);

            if (project is null) return new NotFoundResult();

            var command = new ProjectDeleteCommand(CurrentUser, project);

            var commandResult = await orchestrator
                .InvokeAsync(command)
                .ConfigureAwait(false);

            return commandResult.ActionResult();
        }
    }
}
