using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using SWLOR.Admin.Services;
using SWLOR.Admin.Authorization;

namespace SWLOR.Admin
{
    public class Program
    {
        public static async Task Main(string[] args)
        {

            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();
            builder.Services.AddSingleton<WebSettings>();
            builder.Services.AddSingleton<DiscordSettings>();
            builder.Services.AddSingleton<AuthenticationSettings>();
            builder.Services.AddScoped<DiscordAuthService>();
            builder.Services.AddHttpClient();

            // Get authentication settings to determine if Discord auth is required
            var authSettings = new AuthenticationSettings(builder.Configuration);

            // Configure authentication conditionally
            if (authSettings.RequireDiscordAuth)
            {
                // Production: Full Discord authentication
                var discordSettings = new DiscordSettings(builder.Configuration);
                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = "Discord";
                })
                .AddCookie(options =>
                {
                    options.LoginPath = "/login";
                    options.LogoutPath = "/logout";
                    options.AccessDeniedPath = "/login"; // Redirect to login instead of non-existent Account/AccessDenied
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
                })
                .AddOAuth("Discord", options =>
                {
                    options.ClientId = discordSettings.ClientId;
                    options.ClientSecret = discordSettings.ClientSecret;
                    options.CallbackPath = "/signin-discord";
                    options.AuthorizationEndpoint = "https://discord.com/api/oauth2/authorize";
                    options.TokenEndpoint = "https://discord.com/api/oauth2/token";
                    options.UserInformationEndpoint = "https://discord.com/api/v10/users/@me";
                    
                    options.Scope.Add("identify");
                    options.Scope.Add("guilds");

                    // Claims mapping will be done in the OnCreatingTicket event

                    options.Events = new OAuthEvents
                    {
                        OnCreatingTicket = async context =>
                        {
                            var request = new HttpRequestMessage(HttpMethod.Get, context.Options.UserInformationEndpoint);
                            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
                            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", context.AccessToken);

                            var response = await context.Backchannel.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.HttpContext.RequestAborted);
                            response.EnsureSuccessStatusCode();

                            var userJson = await response.Content.ReadAsStringAsync();
                            var user = JsonDocument.Parse(userJson);

                            // Validate Discord permissions ONCE during login
                            var discordAuthService = context.HttpContext.RequestServices.GetRequiredService<DiscordAuthService>();
                            var tempUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
                            {
                                new Claim(ClaimTypes.NameIdentifier, user.RootElement.GetProperty("id").GetString() ?? ""),
                                new Claim("access_token", context.AccessToken ?? "")
                            }));
                            
                            var hasPermission = await discordAuthService.ValidateUserPermissions(tempUser);
                            
                            // Store permission result in claims for future use
                            context.Identity.AddClaim(new Claim("discord_admin_permission", hasPermission.ToString()));
                            
                            // If user doesn't have permission, fail the authentication
                            if (!hasPermission)
                            {
                                Console.WriteLine("DEBUG: Authentication failed - User does not have required Discord permissions");
                                context.Fail("You do not have the required Discord permissions to access this application.");
                                return;
                            }

                            // Manually add claims
                            if (user.RootElement.TryGetProperty("id", out var id))
                                context.Identity?.AddClaim(new Claim(ClaimTypes.NameIdentifier, id.GetString() ?? ""));
                            
                            if (user.RootElement.TryGetProperty("username", out var username))
                                context.Identity?.AddClaim(new Claim(ClaimTypes.Name, username.GetString() ?? ""));
                            
                            if (user.RootElement.TryGetProperty("email", out var email))
                                context.Identity?.AddClaim(new Claim(ClaimTypes.Email, email.GetString() ?? ""));
                            
                            if (user.RootElement.TryGetProperty("avatar", out var avatar))
                                context.Identity?.AddClaim(new Claim("urn:discord:avatar", avatar.GetString() ?? ""));
                            
                            if (user.RootElement.TryGetProperty("discriminator", out var discriminator))
                                context.Identity?.AddClaim(new Claim("urn:discord:discriminator", discriminator.GetString() ?? ""));

                            // Store the access token for later use
                            context.Identity?.AddClaim(new Claim("access_token", context.AccessToken ?? ""));
                        }
                    };
                });
            }

            builder.Services.AddScoped<IAuthorizationHandler, DiscordAuthorizationHandler>();
            
            // Configure authorization conditionally
            if (authSettings.RequireDiscordAuth)
            {
                // Production: Require authentication
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("DiscordAdmin", policy =>
                        policy.Requirements.Add(new DiscordAuthorizationRequirement()));
                    options.FallbackPolicy = new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .AddRequirements(new DiscordAuthorizationRequirement())
                        .Build();
                });
            }
            else
            {
                // Local development: No authentication required
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("DiscordAdmin", policy =>
                        policy.Requirements.Add(new DiscordAuthorizationRequirement()));
                    options.FallbackPolicy = new AuthorizationPolicyBuilder()
                        .AddRequirements(new DiscordAuthorizationRequirement())
                        .Build();
                });
            }

            var app = builder.Build();

            app.Services.GetService<WebSettings>().Load();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            // Conditionally use authentication middleware
            if (authSettings.RequireDiscordAuth)
            {
                app.UseAuthentication();
            }
            app.UseAuthorization();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            // Authentication endpoints (only if Discord auth is required)
            if (authSettings.RequireDiscordAuth)
            {
                app.MapGet("/login", async (HttpContext context) =>
                {
                    await context.ChallengeAsync("Discord", new AuthenticationProperties
                    {
                        RedirectUri = "/"
                    });
                });

                app.MapPost("/logout", async (HttpContext context) =>
                {
                    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    context.Response.Redirect("/");
                });
            }

            LoadSettings();
            await app.RunAsync();

        }

        private static void LoadSettings()
        {

        }
    }
}
