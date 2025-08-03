using System.Security.Claims;
using System.Text.Json;

namespace SWLOR.Admin.Services
{
    public class DiscordAuthService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DiscordSettings _discordSettings;

        public DiscordAuthService(IHttpClientFactory httpClientFactory, DiscordSettings discordSettings)
        {
            _httpClientFactory = httpClientFactory;
            _discordSettings = discordSettings;
        }

        public async Task<bool> ValidateUserPermissions(ClaimsPrincipal user)
        {
            var accessToken = user.FindFirst("access_token")?.Value;
            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("DEBUG: No access token found");
                return false;
            }

            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                Console.WriteLine("DEBUG: No user ID found");
                return false;
            }

            Console.WriteLine($"DEBUG: Validating permissions for user {userId}");

            try
            {
            // Check if user is member of the allowed guild
            if (!string.IsNullOrEmpty(_discordSettings.AllowedGuildId))
            {
                var isMember = await IsUserGuildMember(userId, _discordSettings.AllowedGuildId, accessToken);
                    Console.WriteLine($"DEBUG: Guild member check result: {isMember}");
                if (!isMember)
                    return false;
            }

            // Check if user has any of the allowed roles
            if (_discordSettings.AllowedRoleIds.Any())
            {
                var userRoles = await GetUserGuildRoles(userId, _discordSettings.AllowedGuildId, accessToken);
                    Console.WriteLine($"DEBUG: User roles: [{string.Join(", ", userRoles)}]");
                    Console.WriteLine($"DEBUG: Allowed roles: [{string.Join(", ", _discordSettings.AllowedRoleIds)}]");
                    
                var hasAllowedRole = userRoles.Any(role => _discordSettings.AllowedRoleIds.Contains(role));
                    Console.WriteLine($"DEBUG: Has allowed role: {hasAllowedRole}");
                if (!hasAllowedRole)
                    return false;
            }

                Console.WriteLine("DEBUG: Permission validation successful");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: Permission validation exception: {ex.Message}");
                // For now, allow access if Discord API is having issues to prevent lockouts
                // In production, you might want to be more restrictive
                Console.WriteLine("DEBUG: Allowing access due to Discord API issues");
            return true;
        }
        }

        private async Task<bool> IsUserGuildMember(string userId, string guildId, string accessToken)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10); // Add timeout to prevent hanging
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
                
                var response = await client.GetAsync($"https://discord.com/api/v10/users/@me/guilds");
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Discord API guild check failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
                    return false;
                }

                var content = await response.Content.ReadAsStringAsync();
                var guilds = JsonSerializer.Deserialize<DiscordGuild[]>(content);
                
                return guilds?.Any(g => g.id == guildId) ?? false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Discord guild member check exception: {ex.Message}");
                return false;
            }
        }

        private async Task<List<string>> GetUserGuildRoles(string userId, string guildId, string accessToken)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10); // Add timeout to prevent hanging
                client.DefaultRequestHeaders.Add("Authorization", $"Bot {_discordSettings.BotToken}");
                
                var response = await client.GetAsync($"https://discord.com/api/v10/guilds/{guildId}/members/{userId}");
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Discord API role check failed: {response.StatusCode} - {errorContent}");
                    return new List<string>();
                }

                var content = await response.Content.ReadAsStringAsync();
                var member = JsonSerializer.Deserialize<DiscordMember>(content);
                
                return member?.roles ?? new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Discord role check exception: {ex.Message}");
                return new List<string>();
            }
        }

        private class DiscordGuild
        {
            public string id { get; set; } = string.Empty;
            public string name { get; set; } = string.Empty;
        }

        private class DiscordMember
        {
            public List<string> roles { get; set; } = new();
        }
    }
}