using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SlackBskyUnfurl.Models;
using SlackBskyUnfurl.Models.Bsky;
using SlackBskyUnfurl.Models.Bsky.Responses;
using SlackBskyUnfurl.Models.Slack;
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

            var topContextBlock = this.CreateTopContextBlock();
            unfurl.Blocks.Add(topContextBlock);

            var postTextBlocks = this.CreatePostTextBlocks(unfurlResult);
            postTextBlocks.ToList().ForEach(b => unfurl.Blocks.Add(b));

            if (unfurlResult.Thread.Post.Embed != null) {
                if (unfurlResult.Thread.Post.Embed.External != null) {
                    var contextBlock = this.CreateLinkContext(unfurlResult.Thread.Post.Embed.External.Uri);
                    unfurl.Blocks.Add(contextBlock);
                    var externalBlock = CreateExternalBlock(unfurlResult);
                    unfurl.Blocks.Add(externalBlock);
                }

                // If the post has images, add them.
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

                // If the post has media, add it.
                // This happens when a post with an image is a repost of another post
                if (unfurlResult.Thread.Post.Embed.Media?.Images != null && unfurlResult.Thread.Post.Embed.Media.Images.Any()) {
                    var mediaImages = unfurlResult.Thread.Post.Embed.Media.Images;
                    foreach (var mediaImage in mediaImages) {
                        unfurl.Blocks.Add(new ImageBlock
                        {
                            ImageUrl = mediaImage.Thumb,
                            AltText = mediaImage.Alt
                        });
                    }
                }

                // I honestly have no idea why there are different types of embeds here, bluesky seems very inconsistent for some reason
                var embedRecord = unfurlResult.Thread.Post.Embed.Record?.Record ?? unfurlResult.Thread.Post.Embed.Record;
                if (embedRecord != null) {
                    var text = this.GetPostTest(embedRecord.Value);
                    // Add block for sub record author
                    var contentBlock = new SectionBlock {
                        Text = new Markdown(
                            $@">>> {this.GetAuthorLine(embedRecord.Author)}{"\n"}{text}"),
                    };

                    unfurl.Blocks.Add(contentBlock);

                    // Add link if the sub record references yet another record
                    if (embedRecord.Embeds.Any(e => e.Record != null)) {
                        var recordEmbed = embedRecord.Embeds.FirstOrDefault(e => e.Record != null);
                        if (recordEmbed != null) {
                            var linkToPost = new SectionBlock {
                                Text = new Markdown($@">>> {new Link(recordEmbed?.External.Uri, recordEmbed?.External?.Uri)}")
                            };
                            unfurl.Blocks.Add(linkToPost);
                        }
                    }

                    // If the sub record has an external link, add it
                    if (embedRecord.Embeds.Any(e => e.External != null)) {
                        var externalEmbed = embedRecord.Embeds.FirstOrDefault(e => e.External != null);
                        if (externalEmbed != null) {
                            var contextBlock = this.CreateLinkContext(externalEmbed.External.Uri, true);
                            unfurl.Blocks.Add(contextBlock);

                            var linkToPost = new SectionBlock {
                                Text = new Markdown(
                                    $@">>> *{Link.Url(externalEmbed.External.Uri, externalEmbed.External.Title)}*{"\n"}{externalEmbed.External.Description}"),
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
                    if (embedRecord.Embeds.Any(e => e.Images != null && e.Images.Any()))
                    {
                        var imagesEmbed = embedRecord.Embeds.Where(e => e.Images != null && e.Images.Any())
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

    protected ContextBlock CreateLinkContext(string uri, bool isNested = false) {
        var url = new Uri(uri);
        var text = $@"{url.Host}";
        var context = new ContextBlock {
            Elements = new List<IContextElement>
                { new ContextTextBlock { Type = "plain_text", Text = $@"{text}" } }
        };
        return context;
    }

    protected ContextBlock CreateTopContextBlock() {
        return new ContextBlock
        {
            Elements = new List<IContextElement>
            {  
                new ContextImageBlock {
                    ImageUrl = "https://slack-bluesky-unfurl.azurewebsites.net/images/logo.png",
                    AltText = "Bluesky Social Logo"
                }, 
                new ContextTextBlock {
                    Type = "mrkdwn", 
                    Text = $@"Bluesky Social"
                }
            }
        };
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
        var text = this.GetPostTest(postThread.Thread.Post.Record);

        var mainTextBlock = new SectionBlock {
            Text = new Markdown(
                $@"{this.GetAuthorLine(postThread.Thread.Post.Author)}{"\n"}{text}"),
        };

        return new List<Block> {
            mainTextBlock,
        };
    }

    protected string GetPostTest(Post post) {
        var text = post.Text;
        var facets = post.Facets;

        if (facets == null || !facets.Any()) {
            return text;
        }

        var linkFacets = facets.Where(f => f.Features.Any(ff => ff.Type.Contains("link"))).ToList();
        linkFacets.ForEach(lf => {
            var substringBytes = Encoding.UTF8.GetBytes(text).Take(new Range(lf.Index.ByteStart, lf.Index.ByteEnd));
            var link = lf.Features.First(f => !string.IsNullOrEmpty(f.Uri)).Uri;
            var substring = Encoding.UTF8.GetString(substringBytes.ToArray(), 0, substringBytes.Count());
            text = text.Replace(substring, Link.Url(link, substring).ToString());
        });

        return text;
    }

    protected string GetAuthorLine(Author author) {
        return $@"*{Link.Url($"https://bsky.app/profile/{author.Handle}", author.DisplayName)}* (@{author.Handle})";
    }
}