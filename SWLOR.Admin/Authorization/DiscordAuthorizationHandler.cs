using Microsoft.AspNetCore.Authorization;

namespace SWLOR.Admin.Authorization
{
    public class DiscordAuthorizationRequirement : IAuthorizationRequirement
    {
    }

    public class DiscordAuthorizationHandler : AuthorizationHandler<DiscordAuthorizationRequirement>
    {
        public DiscordAuthorizationHandler()
        {
        }

        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            DiscordAuthorizationRequirement requirement)
        {
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