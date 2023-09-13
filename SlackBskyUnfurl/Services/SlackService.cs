using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SlackBskyUnfurl.Models;
using SlackBskyUnfurl.Models.Bsky.Responses;
using SlackBskyUnfurl.Services.Interfaces;
using SlackNet;
using SlackNet.Blocks;
using SlackNet.Events;

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
            Models.Slack.LinkShared linkSharedEvent = null;
            try {
                linkSharedEvent = JsonConvert.DeserializeObject<Models.Slack.LinkShared>(json.ToString());
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

    public async Task HandleLinkSharedAsync(Models.Slack.LinkShared linkSharedEvent) {
        if (!linkSharedEvent.Links.Any(l => l.Url.Contains("bsky.app"))) {
            return;
        }

        foreach (var link in linkSharedEvent.Links) {
            this._logger.LogInformation($"Unfurling link {link.Url} in channel {linkSharedEvent.Channel} at timestamp {linkSharedEvent.MessageTs}");

            var unfurlResult = await this._blueSky.HandleGetPostThreadRequest(link.Url);
            if (unfurlResult == null) {
                throw new InvalidOperationException("No result from post.");
            }
            var unfurl = new Attachment {
                Blocks = new List<Block>()
            };

            var postTextBlocks = this.CreatePostTextBlocks(unfurlResult);
            postTextBlocks.ToList().ForEach(b => unfurl.Blocks.Add(b));

            if (unfurlResult.Thread.Post.Embed != null) {
                if (unfurlResult.Thread.Post.Embed.External != null) {
                    var externalBlock = CreateExternalBlock(unfurlResult);
                    unfurl.Blocks.Add(externalBlock);
                }

                if (unfurlResult.Thread.Post.Embed.Images != null && unfurlResult.Thread.Post.Embed.Images.Any())
                {
                    foreach (var image in unfurlResult.Thread.Post.Embed.Images)
                    {
                        unfurl.Blocks.Add(new ImageBlock
                        {
                            ImageUrl = image.Thumb,
                            AltText = image.Alt
                        });
                    }
                }

                if (unfurlResult.Thread.Post.Embed.Record != null) {
                    var externalRecordView = unfurlResult.Thread.Post.Embed.Record;

                    // Add block for sub record author
                    var contentBlock = new SectionBlock {
                        Text = new Markdown(
                            $@">>> {Link.Url($"https://bsky.app/profile/{externalRecordView.Author.Handle}", externalRecordView.Author.DisplayName)}{"\n"}{externalRecordView.Value.Text}"),
                    };

                    unfurl.Blocks.Add(contentBlock);

                    // Add link if the sub record references yet another record
                    if (externalRecordView.Embeds.Any(e => e.Record != null)) {
                        var recordEmbed = externalRecordView.Embeds.FirstOrDefault(e => e.Record != null);
                        if (recordEmbed != null) {
                            var linkToPost = new SectionBlock {
                                Text = new Markdown($@">>> {new Link(recordEmbed?.Record?.Uri, recordEmbed?.Record?.Uri)}")
                            };
                            unfurl.Blocks.Add(linkToPost);
                        }
                    }

                    // If the sub record has an external link, add it
                    if (externalRecordView.Embeds.Any(e => e.External != null)) {
                        var externalEmbed = externalRecordView.Embeds.FirstOrDefault(e => e.External != null);
                        if (externalEmbed != null) {
                            var linkToPost = new SectionBlock {
                                Text = new Markdown(
                                    $@">>> {Link.Url(externalEmbed.External.Uri, externalEmbed.External.Title)}{"\n"}{externalEmbed.External.Description}"),
                                Accessory = new Image
                                {
                                    ImageUrl = externalEmbed.External.Thumb,
                                    AltText = externalEmbed.External.Title
                                }
                            };
                            unfurl.Blocks.Add(linkToPost);
                        }
                    }

                    //If the sub record has images, add them
                    if (externalRecordView.Embeds.Any(e => e.Images != null && e.Images.Any()))
                    {
                        var imagesEmbed = externalRecordView.Embeds.Where(e => e.Images != null && e.Images.Any())
                            .ToList();
                        if (imagesEmbed.Any())
                        {
                            foreach (var image in imagesEmbed
                                         .Where(images => images.Images != null && images.Images.Any())
                                         .SelectMany(images => images.Images ?? Array.Empty<ImageView>()))
                            {
                                unfurl.Blocks.Add(new ImageBlock
                                {
                                    ImageUrl = image.Thumb,
                                    AltText = image.Alt
                                });
                            }
                        }
                    }
                }
            }

            var unfurls = new Dictionary<string, Attachment> {
                {link.Url, unfurl}
            };

            this._logger.LogInformation($"Unfurl Result: {JsonConvert.SerializeObject(unfurl)}");

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

    protected static SectionBlock CreateExternalBlock(GetPostThreadResponse unfurlResult) {
        var externalBlock = new SectionBlock {
            Text = new Markdown(
                $@"{Link.Url(unfurlResult.Thread.Post.Embed.External.Uri, unfurlResult.Thread.Post.Embed.External.Title)}{"\n"}{unfurlResult.Thread.Post.Embed.External.Description}"),
            Accessory = new Image
            {
                ImageUrl = unfurlResult.Thread.Post.Embed.External.Thumb,
                AltText = unfurlResult.Thread.Post.Embed.External.Title
            }
        };
        return externalBlock;
    }

    protected IEnumerable<Block> CreatePostTextBlocks(GetPostThreadResponse postThread) {
        var mainTextBlock = new SectionBlock {
            Text = new Markdown(
                $@"{Link.Url($"https://bsky.app/profile/{postThread.Thread.Post.Author.Handle}", postThread.Thread.Post.Author.DisplayName)}{"\n"}{postThread.Thread.Post.Record.Text}"),
        };

        return new List<Block> {
            mainTextBlock,
        };
    }
}