using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SlackBskyUnfurl.Data;
using SlackBskyUnfurl.Data.Models;
using SlackBskyUnfurl.Models.Slack;
using SlackBskyUnfurl.Services.Interfaces;
using SlackNet;
using SlackNet.Events;
using LinkShared = SlackBskyUnfurl.Models.Slack.LinkShared;

namespace SlackBskyUnfurl.Services;

public class SlackService : ISlackService {
    private readonly IBlueSkyService _blueSky;
    private readonly IConfiguration _configuration;
    private readonly SlackBskyContext _dbcontext;
    private readonly ILogger<SlackService> _logger;
    public ISlackApiClient? Client;

    public SlackService(IBlueSkyService blueSkyService, IConfiguration configuration,
        ILogger<SlackService> logger) {
        this._blueSky = blueSkyService;
        this._configuration = configuration;
        this._logger = logger;

        var contextOptions = new DbContextOptionsBuilder<SlackBskyContext>()
            .UseSqlServer(this._configuration.GetConnectionString("Remote"))
            .Options;
        this._dbcontext = new SlackBskyContext(contextOptions);
    }

    public async Task<bool> SaveAccessToken(ScopeResponse accessResponse) {
        try {
            var existingWorkspace =
                await this._dbcontext.AuthorizedWorkspaces.FirstOrDefaultAsync(w => w.TeamId == accessResponse.Team.Id);
            if (existingWorkspace != null) {
                existingWorkspace.AccessToken = accessResponse.AccessToken;
                try {
                    await this._dbcontext.SaveChangesAsync();
                }
                catch (Exception e) {
                    throw new InvalidOperationException(
                        $"Error updating access token for team {accessResponse.Team.Id}",
                        e);
                }
            }
            else {
                try {
                    this._dbcontext.AuthorizedWorkspaces.Add(new AuthorizedWorkspaceEntity {
                        Id = Guid.NewGuid(),
                        TeamId = accessResponse.Team.Id,
                        AccessToken = accessResponse.AccessToken
                    });
                    await this._dbcontext.SaveChangesAsync();
                }
                catch (Exception e) {
                    throw new InvalidOperationException(
                        $"Error creating access token for team {accessResponse.Team.Id}", e);
                }
            }
        }
        catch (Exception e) {
            throw new InvalidOperationException($"Error attempting to retrieve token for team {accessResponse.Team.Id}",
                e);
        }

        return true;
    }

    public async Task<string> HandleVerification(UrlVerification slackEvent) {
        return slackEvent.Challenge;
    }

    public async Task HandleIncomingEvent(JsonElement dynamicSlackEvent) {
        var contractResolver = new DefaultContractResolver {
            NamingStrategy = new SnakeCaseNamingStrategy()
        };
        var slackEvent = JsonConvert.DeserializeObject<EventCallback>(dynamicSlackEvent.ToString(),
            new JsonSerializerSettings {
                ContractResolver = contractResolver,
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore
            });
        if (slackEvent.Event.Type == "link_shared") {
            var json = dynamicSlackEvent.GetProperty("event").ToString();
            LinkShared linkSharedEvent = null;
            try {
                linkSharedEvent = JsonConvert.DeserializeObject<LinkShared>(json);
            }
            catch (Exception e) {
                throw new InvalidOperationException("Invalid json", e);
            }

            if (linkSharedEvent == null) {
                return;
            }

            if (string.IsNullOrEmpty(slackEvent.TeamId)) {
                throw new InvalidOperationException("TeamId is null or empty");
            }

            await this.SetClientToken(slackEvent.TeamId);

            await this.HandleLinkSharedAsync(linkSharedEvent);
        }
    }

    public async Task HandleLinkSharedAsync(LinkShared linkSharedEvent) {
        if (!linkSharedEvent.Links.Any(l => l.Url.Contains("bsky.app"))) {
            return;
        }

        if (this.Client == null) {
            throw new InvalidOperationException("Slack client is null");
        }

        foreach (var link in linkSharedEvent.Links) {
            this._logger.LogInformation(
                $"Unfurling link {link.Url} in channel {linkSharedEvent.Channel} at timestamp {linkSharedEvent.MessageTs}");

            var unfurlResult = await this._blueSky.HandleGetPostThreadRequest(link.Url);
            if (unfurlResult == null) {
                throw new InvalidOperationException("No result from post.");
            }

            var unfurl = SlackBlockCreator.CreateBlueSkyUnfurl(unfurlResult);

            var unfurls = new Dictionary<string, Attachment> {
                { link.Url, unfurl }
            };

            this._logger.LogDebug($"Unfurl Result: {JsonConvert.SerializeObject(unfurl, new JsonSerializerSettings {
                MetadataPropertyHandling = MetadataPropertyHandling.Ignore
            })}");

            try {
                await this.Client.Chat.Unfurl(
                    linkSharedEvent.Channel,
                    linkSharedEvent.MessageTs,
                    unfurls
                );
            }
            catch (Exception e) {
                this._logger.LogError(e, "Error sending unfurl to slack");
            }
        }
    }

    private async Task<string> GetAccessToken(string teamId) {
        try {
            var workspace = await this._dbcontext.AuthorizedWorkspaces.FirstOrDefaultAsync(w => w.TeamId == teamId);
            if (workspace == null || string.IsNullOrEmpty(workspace.AccessToken)) {
                this._logger.LogError($"No access token found for team {teamId}");
                throw new InvalidOperationException($"No access token found for team {teamId}");
            }

            return workspace.AccessToken;
        }
        catch (Exception e) {
            this._logger.LogError(e, $"Error fetching access token for team {teamId}");
            throw new InvalidOperationException($"Error fetching access token for team {teamId}", e);
        }
    }

    private async Task SetClientToken(string teamId) {
        var accessToken = await this.GetAccessToken(teamId);
        this.Client = new SlackServiceBuilder().UseApiToken(accessToken).GetApiClient();
    }
}