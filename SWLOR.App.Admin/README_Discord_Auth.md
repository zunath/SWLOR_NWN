# Discord Authentication Setup for SWLOR Admin

This document explains how to configure Discord OAuth2 authentication for the SWLOR Admin site.

## Prerequisites

1. A Discord application and bot registered at https://discord.com/developers/applications
2. A Discord server where you want to restrict access to specific users/roles

## Discord Application Setup

### 1. Create Discord Application
1. Go to https://discord.com/developers/applications
2. Click "New Application" and give it a name (e.g., "SWLOR Admin")
3. Navigate to the "OAuth2" section

### 2. Configure OAuth2 Settings
1. In the OAuth2 section, add the following redirect URI:
   - `https://yourdomain.com/signin-discord` (replace with your actual domain)
   - For local development: `https://localhost:5001/signin-discord`

2. Under "OAuth2 URL Generator":
   - Select scopes: `identify` and `guilds`
   - Copy the generated URL for testing

### 3. Get Client Credentials
1. Copy the "Client ID" from the OAuth2 section
2. Copy the "Client Secret" (click "Reset Secret" if needed)

### 4. Create Bot (Optional - for role checking)
1. Navigate to the "Bot" section
2. Click "Add Bot" if not already created
3. Copy the bot token
4. Enable "Server Members Intent" if you plan to check roles

### 5. Get Guild and Role IDs
1. Enable Developer Mode in Discord (User Settings > Advanced > Developer Mode)
2. Right-click your server name and select "Copy ID" - this is your Guild ID
3. Right-click any role you want to grant admin access and select "Copy ID"

## Application Configuration

### 1. Update appsettings.json or appsettings.Development.json

```json
{
  "Discord": {
    "ClientId": "YOUR_DISCORD_CLIENT_ID",
    "ClientSecret": "YOUR_DISCORD_CLIENT_SECRET",
    "BotToken": "YOUR_DISCORD_BOT_TOKEN",
    "AllowedGuildId": "YOUR_DISCORD_GUILD_ID",
    "AllowedRoleIds": ["ADMIN_ROLE_ID_1", "ADMIN_ROLE_ID_2"]
  }
}
```

### 2. Configuration Options

- **ClientId**: OAuth2 Client ID from Discord application
- **ClientSecret**: OAuth2 Client Secret from Discord application
- **BotToken**: Bot token (required only if checking roles)
- **AllowedGuildId**: Discord server ID where users must be members (optional)
- **AllowedRoleIds**: Array of role IDs that grant admin access (optional)

### 3. Security Considerations

- If `AllowedGuildId` is empty, any Discord user can potentially access the admin (not recommended)
- If `AllowedRoleIds` is empty but `AllowedGuildId` is set, any member of that guild can access the admin
- For maximum security, set both `AllowedGuildId` and `AllowedRoleIds`

## Features

### Authentication Flow
1. Users visit the admin site
2. Unauthenticated users are redirected to `/login`
3. Users click "Login with Discord" to authenticate
4. Discord redirects back with user information
5. The application checks guild membership and roles
6. Access is granted or denied based on configuration

### Authorization
- All admin pages require the "DiscordAdmin" policy
- The policy validates Discord guild membership and roles
- Failed authorization redirects to access denied page

### User Interface
- Login/logout display in the navigation bar
- User's Discord username and discriminator shown when logged in
- Friendly login page with Discord branding
- Access denied messaging for unauthorized users

## Development

### Running Locally
1. Set up your Discord application with `https://localhost:5001/signin-discord` as a redirect URI
2. Update `appsettings.Development.json` with your Discord credentials
3. Run the application with `dotnet run`
4. Navigate to `https://localhost:5001`

### Testing
- Test with users who have the required roles
- Test with users who don't have required roles
- Test with users who aren't in the specified guild
- Verify logout functionality works correctly

## Troubleshooting

### Common Issues
1. **Redirect URI mismatch**: Ensure the redirect URI in Discord matches exactly what you configured
2. **Invalid client credentials**: Double-check Client ID and Client Secret
3. **Role checking fails**: Ensure the bot token is correct and has proper permissions
4. **Users can't access**: Verify the user has the required roles and is in the specified guild

### Logs
Check the application logs for authentication errors. Common error patterns:
- OAuth2 errors indicate Discord configuration issues
- Authorization failures indicate role/guild permission issues