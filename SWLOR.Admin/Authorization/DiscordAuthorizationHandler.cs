using Microsoft.AspNetCore.Authorization;

namespace SWLOR.Admin.Authorization
{
    public class DiscordAuthorizationRequirement : IAuthorizationRequirement
    {
    }

    public class DiscordAuthorizationHandler : AuthorizationHandler<DiscordAuthorizationRequirement>
    {
        private readonly AuthenticationSettings _authSettings;

        public DiscordAuthorizationHandler(AuthenticationSettings authSettings)
        {
            _authSettings = authSettings;
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            DiscordAuthorizationRequirement requirement)
        {
            // If Discord auth is not required (e.g., local development), allow access
            if (!_authSettings.RequireDiscordAuth)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            // Original Discord authentication logic for production
            if (context.User.Identity?.IsAuthenticated == true)
            {
                // Check the stored permission claim instead of making API calls
                var permissionClaim = context.User.FindFirst("discord_admin_permission")?.Value;
                if (permissionClaim == "True")
                {
                    context.Succeed(requirement);
                }
                else
                {
                    context.Fail();
                }
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}