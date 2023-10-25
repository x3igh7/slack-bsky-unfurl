using System.Text.Json;
using Newtonsoft.Json;
using SlackBskyUnfurl.Models;
using SlackBskyUnfurl.Services.Interfaces;
using SlackNet;
using SlackNet.Blocks;
using SlackNet.Events;
using LinkShared = SlackBskyUnfurl.Models.Slack.LinkShared;

namespace SlackBskyUnfurl.Services;

public class SlackService : ISlackService {
    private readonly IBlueSkyService _blueSky;
    private readonly ILogger<SlackService> _logger;
    public ISlackApiClient Client;

    public SlackService(IBlueSkyService blueSkyService, IConfiguration configuration, ILogger<SlackService> logger) {
        this._blueSky = blueSkyService;
        this._logger = logger;

        var apiToken = configuration["SlackApiToken"];
        var clientSecret = configuration["SlackClientSecret"];
        var signingSecret = configuration["SlackSigningSecret"];
        this.Client = new SlackServiceBuilder().UseApiToken(apiToken).GetApiClient();
    }

    public async Task<string> HandleVerification(UrlVerification slackEvent) {
        return slackEvent.Challenge;
    }

    public async Task HandleIncomingEvent(JsonElement dynamicSlackEvent) {
        var slackEvent = JsonConvert.DeserializeObject<EventCallback>(dynamicSlackEvent.ToString());
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

            await this.HandleLinkSharedAsync(linkSharedEvent);
        }
    }

    public async Task HandleLinkSharedAsync(LinkShared linkSharedEvent) {
        if (!linkSharedEvent.Links.Any(l => l.Url.Contains("bsky.app"))) {
            return;
        }

        foreach (var link in linkSharedEvent.Links) {
            this._logger.LogInformation(
                $"Unfurling link {link.Url} in channel {linkSharedEvent.Channel} at timestamp {linkSharedEvent.MessageTs}");

            var unfurlResult = await this._blueSky.HandleGetPostThreadRequest(link.Url);
            if (unfurlResult == null) {
                throw new InvalidOperationException("No result from post.");
            }

            var unfurl = new Attachment {
                Blocks = new List<Block>()
            };

            var topContextBlock = SlackBlockCreator.CreateTopContextBlock();
            unfurl.Blocks.Add(topContextBlock);

            var postTextBlock = SlackBlockCreator.CreatePostTextBlock(unfurlResult);
            unfurl.Blocks.Add(postTextBlock);

            if (unfurlResult.Thread.Post.Embed != null) {
                if (unfurlResult.Thread.Post.Embed.External != null) {
                    var contextBlock =
                        SlackBlockCreator.CreateLinkContextBlock(unfurlResult.Thread.Post.Embed.External.Uri);
                    unfurl.Blocks.Add(contextBlock);

                    var externalBlock = SlackBlockCreator.CreateExternalBlock(unfurlResult);
                    unfurl.Blocks.Add(externalBlock);
                }

                // If the post has images, add them.
                if (unfurlResult.Thread.Post.Embed.Images != null && unfurlResult.Thread.Post.Embed.Images.Any()) {
                    var imageBlocks = SlackBlockCreator.CreateImageViewBlocks(unfurlResult.Thread.Post.Embed.Images);
                    imageBlocks.ForEach(i => unfurl.Blocks.Add(i));
                }

                // If the post has media, add it.
                // This happens when a post with an image is a repost of another post
                if (unfurlResult.Thread.Post.Embed.Media?.Images != null &&
                    unfurlResult.Thread.Post.Embed.Media.Images.Any()) {
                    var mediaImages = unfurlResult.Thread.Post.Embed.Media.Images;
                    var imageBlocks = SlackBlockCreator.CreateImageViewBlocks(mediaImages);
                    imageBlocks.ForEach(i => unfurl.Blocks.Add(i));
                }

                // I honestly have no idea why there are different types of embeds here, bluesky seems very inconsistent for some reason on this
                var embedRecord = unfurlResult.Thread.Post.Embed.Record?.Record ??
                                  unfurlResult.Thread.Post.Embed.Record;
                if (embedRecord != null) {
                    var embedTextBlock = SlackBlockCreator.CreateRecordViewTextBlock(embedRecord, true);
                    unfurl.Blocks.Add(embedTextBlock);

                    // Add link if the sub record references yet another record
                    if (embedRecord.Embeds.Any(e => e.Record != null)) {
                        var recordEmbed = embedRecord.Embeds.FirstOrDefault(e => e.Record != null);
                        if (recordEmbed != null) {
                            var recordLinkBlock = SlackBlockCreator.CreateEmbedViewRecordLinkBlock(recordEmbed);
                            unfurl.Blocks.Add(recordLinkBlock);
                        }
                    }

                    // If the sub record has an external link, add it
                    if (embedRecord.Embeds.Any(e => e.External != null)) {
                        var externalEmbed = embedRecord.Embeds.FirstOrDefault(e => e.External != null);
                        if (externalEmbed != null) {
                            var contextBlock =
                                SlackBlockCreator.CreateLinkContextBlock(externalEmbed.External.Uri, true);
                            unfurl.Blocks.Add(contextBlock);

                            var linkToPost = SlackBlockCreator.CreateEmbedExternalLinkBlock(externalEmbed);
                            unfurl.Blocks.Add(linkToPost);
                        }
                    }

                    //If the sub record has images, add them
                    if (embedRecord.Embeds.Any(e => e.Images != null && e.Images.Any())) {
                        var embedsWithImages = embedRecord.Embeds.Where(e => e.Images != null && e.Images.Any())
                            .ToList();
                        if (embedsWithImages.Any()) {
                            var embedImages = embedsWithImages
                                .Where(images => images.Images != null && images.Images.Any())
                                .SelectMany(images => images.Images ?? Array.Empty<ImageView>());
                            var imageBlocks = SlackBlockCreator.CreateImageViewBlocks(embedImages);
                            imageBlocks.ForEach(i => unfurl.Blocks.Add(i));
                        }
                    }
                }
            }

            var unfurls = new Dictionary<string, Attachment> {
                { link.Url, unfurl }
            };

            this._logger.LogDebug($"Unfurl Result: {JsonConvert.SerializeObject(unfurl)}");

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
}