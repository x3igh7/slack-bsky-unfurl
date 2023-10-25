using SlackBskyUnfurl.Models.Bsky.Responses;
using SlackBskyUnfurl.Models.Bsky;
using SlackBskyUnfurl.Models.Slack;
using SlackBskyUnfurl.Models;
using SlackNet.Blocks;
using SlackNet;
using System.Text;

namespace SlackBskyUnfurl.Services
{
    public static class SlackBlockCreator
    {
        public static SectionBlock CreateEmbedExternalLinkBlock(EmbedView externalEmbed)
        {
            var linkToPost = new SectionBlock
            {
                Text = new Markdown(
                    $@">>> *{Link.Url(externalEmbed.External.Uri, externalEmbed.External.Title)}*{"\n"}{externalEmbed.External.Description}"),
                Accessory = new Image
                {
                    ImageUrl = externalEmbed.External.Thumb,
                    AltText = externalEmbed.External.Title
                }
            };
            return linkToPost;
        }

        public static SectionBlock CreateEmbedViewRecordLinkBlock(EmbedView embedRecord) {
            var postId = embedRecord.Record.Uri.Split("/").Last();
            var url = $"https://bsky.app/profile/{embedRecord.Record.Author.Handle}/post/{postId}";
            var linkToPost = new SectionBlock
            {
                Text = new Markdown($@">>>{new Link(url, url)}")
            };

            return linkToPost;
        }

        public static List<ImageBlock> CreateImageViewBlocks(IEnumerable<ImageView> images)
        {
            var imageBlocks = new List<ImageBlock>();
            imageBlocks.AddRange(images.Select(image => new ImageBlock { ImageUrl = image.Thumb, AltText = image.Alt }));

            return imageBlocks;
        }

        public static ContextBlock CreateLinkContextBlock(string uri, bool isNested = false)
        {
            var url = new Uri(uri);
            var text = $@"{url.Host}";
            var context = new ContextBlock
            {
                Elements = new List<IContextElement>
                { new ContextTextBlock { Type = "plain_text", Text = $@"{text}" } }
            };
            return context;
        }

        public static ContextBlock CreateTopContextBlock()
        {
            return new ContextBlock
            {
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

        public static SectionBlock CreateExternalBlock(GetPostThreadResponse unfurlResult)
        {
            var externalBlock = new SectionBlock
            {
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

        public static Block CreatePostTextBlock(GetPostThreadResponse postThread)
        {
            var text = GetPostText(postThread.Thread.Post.Record);

            var mainTextBlock = new SectionBlock
            {
                Text = new Markdown(
                    $@"{GetAuthorLine(postThread.Thread.Post.Author)}{"\n"}{text}")
            };

            return mainTextBlock;
        }

        public static Block CreateRecordViewTextBlock(RecordView record, bool isNested = false)
        {
            var text = GetPostText(record.Value);
            var nestedText = isNested ? ">>> " : string.Empty;

            var mainTextBlock = new SectionBlock
            {
                Text = new Markdown($@"{nestedText}{GetAuthorLine(record.Author)}{"\n"}{text}")
            };

            return mainTextBlock;
        }

        private static string GetPostText(Post post)
        {
            var text = post.Text;
            var facets = post.Facets;

            if (facets == null || !facets.Any())
            {
                return text;
            }

            var linkFacets = facets.Where(f => f.Features.Any(ff => ff.Type.Contains("link"))).ToList();
            linkFacets.ForEach(lf => { text = ReplaceLinkFacetText(text, lf); });

            return text;
        }

        private static string ReplaceLinkFacetText(string text, Facet lf)
        {
            var substringBytes = Encoding.UTF8.GetBytes(text).Take(new Range(lf.Index.ByteStart, lf.Index.ByteEnd));
            var link = lf.Features.First(f => !string.IsNullOrEmpty(f.Uri)).Uri;
            var substring = Encoding.UTF8.GetString(substringBytes.ToArray(), 0, substringBytes.Count());
            text = text.Replace(substring, Link.Url(link, substring).ToString());
            return text;
        }

        private static string GetAuthorLine(Author author)
        {
            return $@"*{Link.Url($"https://bsky.app/profile/{author.Handle}", author.DisplayName)}* (@{author.Handle})";
        }
    }
}
