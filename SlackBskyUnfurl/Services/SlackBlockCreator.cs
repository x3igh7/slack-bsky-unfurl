using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using SlackBskyUnfurl.Models.Bsky;
using SlackBskyUnfurl.Models.Bsky.Responses;
using SlackBskyUnfurl.Models.Slack;
using SlackNet;
using SlackNet.Blocks;

namespace SlackBskyUnfurl.Services;

public static class SlackBlockCreator {
    public static Attachment CreateBlueSkyUnfurl(GetPostThreadResponse unfurlResult) {
        var unfurl = new Attachment {
            Blocks = new List<Block>()
        };

        var topContextBlock = CreateTopContextBlock();
        unfurl.Blocks.Add(topContextBlock);

        var postTextBlock = CreatePostTextBlock(unfurlResult);
        unfurl.Blocks.Add(postTextBlock);

        if (unfurlResult.Thread.Post.Embed != null) {
            if (unfurlResult.Thread.Post.Embed.External != null) {
                var contextBlock =
                    CreateLinkContextBlock(unfurlResult.Thread.Post.Embed.External.Uri);
                unfurl.Blocks.Add(contextBlock);

                var externalBlock = CreateExternalBlock(unfurlResult);
                unfurl.Blocks.Add(externalBlock);
            }

            // If the post has images, add them.
            if (unfurlResult.Thread.Post.Embed.Images != null && unfurlResult.Thread.Post.Embed.Images.Any()) {
                var imageBlocks = CreateImageViewBlocks(unfurlResult.Thread.Post.Embed.Images);
                imageBlocks.ForEach(i => unfurl.Blocks.Add(i));
            }

            // if the post had video, add it
            if (unfurlResult.Thread.Post.Embed != null &&
                !unfurlResult.Thread.Post.Embed.Playlist.IsNullOrEmpty()) {
                var videoBlock = CreateVideoBlock(unfurlResult.Thread.Post.Embed);
                unfurl.Blocks.Add(videoBlock);
            }

            // If the post has media, add it.
            // This happens when a post with an image is a repost of another post
            if (unfurlResult.Thread.Post.Embed.Media?.Images != null &&
                unfurlResult.Thread.Post.Embed.Media.Images.Any()) {
                var mediaImages = unfurlResult.Thread.Post.Embed.Media.Images;
                var imageBlocks = CreateImageViewBlocks(mediaImages);
                imageBlocks.ForEach(i => unfurl.Blocks.Add(i));
            }

            // check for media video
            if (unfurlResult.Thread.Post.Embed.Media?.Video != null &&
                !unfurlResult.Thread.Post.Embed.Media.Video.Playlist.IsNullOrEmpty()) {
                var mediaVideo = unfurlResult.Thread.Post.Embed.Media.Video;
                var videoBlock = CreateVideoBlock(mediaVideo);
                unfurl.Blocks.Add(videoBlock);
            }

            // I honestly have no idea why there are different types of embeds here, bluesky seems very inconsistent for some reason on this
            var embedRecord = unfurlResult.Thread.Post.Embed.Record?.Record ??
                              unfurlResult.Thread.Post.Embed.Record;
            if (embedRecord != null) {
                var embedTextBlock = CreateRecordViewTextBlock(embedRecord, true);
                unfurl.Blocks.Add(embedTextBlock);

                // Add link if the sub record references yet another record
                if (embedRecord.Embeds.Any(e => e.Record != null)) {
                    var recordEmbed = embedRecord.Embeds.FirstOrDefault(e => e.Record != null);
                    if (recordEmbed != null) {
                        var recordLinkBlock = CreateEmbedViewRecordLinkBlock(recordEmbed);
                        if (recordLinkBlock != null) {
                            unfurl.Blocks.Add(recordLinkBlock);
                        }
                    }
                }

                // If the sub record has an external link, add it
                if (embedRecord.Embeds.Any(e => e.External != null)) {
                    var externalEmbed = embedRecord.Embeds.FirstOrDefault(e => e.External != null);
                    if (externalEmbed != null) {
                        var contextBlock =
                            CreateLinkContextBlock(externalEmbed.External.Uri, true);
                        unfurl.Blocks.Add(contextBlock);

                        var linkToPost = CreateEmbedExternalLinkBlock(externalEmbed);
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
                        var imageBlocks = CreateImageViewBlocks(embedImages);
                        imageBlocks.ForEach(i => unfurl.Blocks.Add(i));
                    }
                }

                // if the sub record has video, add it
                if (embedRecord.Embeds.Any(e => e.Type.Contains("video") && !e.Playlist.IsNullOrEmpty())) {
                    var embedsWithVideo = embedRecord.Embeds
                        .Where(e => e.Type.Contains("video") && !e.Playlist.IsNullOrEmpty())
                        .ToList();
                    if (embedsWithVideo.Any()) {
                        foreach (var videoBlock in embedsWithVideo.Select(CreateVideoBlock)) {
                            unfurl.Blocks.Add(videoBlock);
                        }
                    }
                }
            }
        }

        return unfurl;
    }

    public static SectionBlock CreateEmbedExternalLinkBlock(EmbedView externalEmbed) {
        var descriptionText = Regex.Replace(externalEmbed.External.Description, @"\n", $"{'\n'}");
        if (externalEmbed.External.Thumb.IsNullOrEmpty()) {
            return new SectionBlock {
                Text = new Markdown(
                    $@">>> *{Link.Url(externalEmbed.External.Uri, externalEmbed.External.Title)}*{'\n'}{descriptionText}")
            };
        }

        var linkToPost = new SectionBlock {
            Text = new Markdown(
                $@">>> *{Link.Url(externalEmbed.External.Uri, externalEmbed.External.Title)}*{'\n'}{descriptionText}"),
            Accessory = new Image {
                ImageUrl = externalEmbed.External.Thumb,
                AltText = externalEmbed.External.Title
            }
        };
        return linkToPost;
    }

    public static SectionBlock? CreateEmbedViewRecordLinkBlock(EmbedView embedRecord) {
        // sometimes the record is more deeply embeded and also ensure there's a URI
        if (embedRecord.Record!.Uri.IsNullOrEmpty()) {
            if (embedRecord.Record.Record == null || embedRecord.Record.Record.Uri.IsNullOrEmpty()) {
                return null;
            }

            var deepEmbedRecordPostId = embedRecord.Record.Record.Uri.Split("/").Last();
            var embedUrl =
                $"https://bsky.app/profile/{embedRecord.Record.Record.Author.Handle}/post/{deepEmbedRecordPostId}";

            return new SectionBlock {
                Text = new Markdown($@">>>{new Link(embedUrl, embedUrl)}")
            };
        }

        var postId = embedRecord.Record.Uri.Split("/").Last();
        var url = $"https://bsky.app/profile/{embedRecord.Record.Author.Handle}/post/{postId}";
        var linkToPost = new SectionBlock {
            Text = new Markdown($@">>>{new Link(url, url)}")
        };

        return linkToPost;
    }

    public static List<ImageBlock> CreateImageViewBlocks(IEnumerable<ImageView> images) {
        var imageBlocks = new List<ImageBlock>();
        imageBlocks.AddRange(images.Select(image => new ImageBlock { ImageUrl = image.Thumb, AltText = image.Alt }));

        return imageBlocks;
    }

    // this will actually return an image block as a placeholder for the video since video doesnt seem to work
    public static ImageBlock CreateVideoBlock(VideoView video) {
        if (video.Alt.IsNullOrEmpty()) {
            return new ImageBlock {
                ImageUrl = video.Thumbnail,
                Title = "Video",
                AltText = "Video"
            };
        }

        return new ImageBlock {
            ImageUrl = video.Thumbnail,
            AltText = video.Alt,
            Title = "Video"
        };
    }

    public static ContextBlock CreateLinkContextBlock(string uri, bool isNested = false) {
        var url = new Uri(uri);
        var text = $@"{url.Host}";
        var context = new ContextBlock {
            Elements = new List<IContextElement>
                { new ContextTextBlock { Type = "plain_text", Text = $@"{text}" } }
        };
        return context;
    }

    public static ContextBlock CreateTopContextBlock() {
        return new ContextBlock {
            Elements = new List<IContextElement> {
                new ContextImageBlock {
                    ImageUrl = "https://slack-bluesky-unfurl.azurewebsites.net/images/logo.png",
                    AltText = "Bluesky Social Logo"
                },
                new ContextTextBlock {
                    Type = "mrkdwn",
                    Text = @"Bluesky Social"
                }
            }
        };
    }

    public static SectionBlock CreateExternalBlock(GetPostThreadResponse unfurlResult) {
        var descriptionText = Regex.Replace(unfurlResult.Thread.Post.Embed.External.Description, @"\n", $"{'\n'}");
        if (unfurlResult.Thread.Post.Embed.External.Thumb.IsNullOrEmpty()) {
            return new SectionBlock {
                Text = new Markdown {
                    Text =
                        $@"{Link.Url(unfurlResult.Thread.Post.Embed.External.Uri, unfurlResult.Thread.Post.Embed.External.Title)}{'\n'}{descriptionText}"
                }
            };
        }

        var externalBlock = new SectionBlock {
            Text = new Markdown {
                Text =
                    $@"{Link.Url(unfurlResult.Thread.Post.Embed.External.Uri, unfurlResult.Thread.Post.Embed.External.Title)}{'\n'}{descriptionText}"
            },
            Accessory = new Image {
                ImageUrl = unfurlResult.Thread.Post.Embed.External.Thumb,
                AltText = unfurlResult.Thread.Post.Embed.External.Title
            }
        };
        return externalBlock;
    }

    public static Block CreatePostTextBlock(GetPostThreadResponse postThread) {
        var postText = GetPostText(postThread.Thread.Post.Record);
        postText = Regex.Replace(postText, @"\n", $"{'\n'}");
        var text = $@"{GetAuthorLine(postThread.Thread.Post.Author)}{'\n'}{postText}";
        var sectionText = new Markdown {
            Text = text
        };
        var mainTextBlock = new SectionBlock {
            Text = sectionText
        };

        return mainTextBlock;
    }

    public static Block CreateRecordViewTextBlock(RecordView record, bool isNested = false) {
        var text = GetPostText(record.Value);
        text = Regex.Replace(text, @"\n", $"{'\n'}");

        var nestedText = isNested ? ">>> " : string.Empty;
        nestedText = Regex.Replace(nestedText, @"\n", $"{'\n'}");

        var mainTextBlock = new SectionBlock {
            Text = new Markdown {
                Text = $@"{nestedText}{GetAuthorLine(record.Author)}{'\n'}{text}"
            }
        };

        return mainTextBlock;
    }

    private static string GetPostText(Post post) {
        var text = post.Text;
        var facets = post.Facets;

        if (facets == null || !facets.Any()) {
            return text;
        }

        var linkFacets = facets.Where(f => f.Features.Any(ff => ff.Type.Contains("link"))).ToList();
        linkFacets.ForEach(lf => { text = ReplaceLinkFacetText(text, lf); });

        return text;
    }

    private static string ReplaceLinkFacetText(string text, Facet lf) {
        var substringBytes = Encoding.UTF8.GetBytes(text).Take(new Range(lf.Index.ByteStart, lf.Index.ByteEnd));
        var link = lf.Features.First(f => !string.IsNullOrEmpty(f.Uri)).Uri;
        var substring = Encoding.UTF8.GetString(substringBytes.ToArray(), 0, substringBytes.Count());
        text = text.Replace(substring, Link.Url(link, substring).ToString());
        return text;
    }

    private static string GetAuthorLine(Author author) {
        return $@"*{Link.Url($"https://bsky.app/profile/{author.Handle}", author.DisplayName)}* (@{author.Handle})";
    }
}