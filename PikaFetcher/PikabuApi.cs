using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;
using AngleSharp.Html.Parser;
using Newtonsoft.Json;

namespace PikaFetcher
{
    internal class PikabuApi : IDisposable
    {
        private const string PikabuUri = "https://pikabu.ru";

        private volatile HttpClient _httpClient;

        public async Task Init()
        {
            if (_httpClient == null)
            {
                var cookieContainer = new CookieContainer();
                _httpClient = new HttpClient(new HttpClientHandler
                {
                    CookieContainer = cookieContainer,
                }, true);
                var pikabuUri = new Uri(PikabuUri);
                (await _httpClient.GetAsync(pikabuUri)).EnsureSuccessStatusCode();
                var sessionId = cookieContainer.GetCookies(pikabuUri)
                    .OfType<Cookie>()
                    .First(cookie => cookie.Name == "PHPSESS").Value;
                _httpClient.DefaultRequestHeaders.Add("x-csrf-token", sessionId);
            }
        }

        public async Task<int> GetLatestStoryId()
        {
            if (_httpClient == null)
            {
                throw new InvalidOperationException();
            }

            var htmlParser = new HtmlParser();
            var document = htmlParser.ParseDocument(await _httpClient.GetStringAsync(CreateUri("/new")));
            var latestStoryIdStr = document.Body.QuerySelector("article").GetAttribute("data-story-id");
            var result = int.Parse(latestStoryIdStr);
            return result;
        }

        public async Task<StoryComments> GetStoryComments(int storyId)
        {
            if (_httpClient == null)
            {
                throw new InvalidOperationException();
            }

            var comments = new List<StoryComment>();
            var (storyTitle, author, rating, timestamp, totalCommentsCount) = await LoadRootComments(storyId, comments);

            while (totalCommentsCount > comments.Count)
            {
                await LoadComments(storyId, comments.Last().CommentId, comments);
            }

            return new StoryComments(storyId, author, storyTitle, rating, timestamp, comments);
        }

        private async Task LoadComments(int storyId, long startCommentId, ICollection<StoryComment> comments)
        {
            var formContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("action", "get_story_comments"),
                new KeyValuePair<string, string>("story_id", storyId.ToString()),
                new KeyValuePair<string, string>("start_comment_id", startCommentId.ToString()),
            });

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(PikabuUri + "/ajax/comments_actions.php"),
                Method = HttpMethod.Post,
                Content = formContent
            };
            
            var httpResponseMessage = await _httpClient.SendAsync(request);
            var response = await httpResponseMessage.Content.ReadAsStringAsync();
            var responseObject = (dynamic) JsonConvert.DeserializeObject(response);
            foreach (var comment in responseObject.data.comments)
            {
                var htmlParser = new HtmlParser();
                var document = htmlParser.ParseDocument((string)comment.html);
                foreach (var storyComment in document.Body.QuerySelectorAll("div.comment"))
                {
                    comments.Add(ParseComment(storyComment));
                }
            }
        }

        private async Task<(string storyTitle, string author, int? rating, DateTimeOffset timestamp, int totalCommentsCount)> LoadRootComments(int storyId, List<StoryComment> storyComments)
        {
            var htmlParser = new HtmlParser();
            var document = htmlParser.ParseDocument(await _httpClient.GetStringAsync(CreateUri("/story/_" + storyId)));

            var storyTitle = document.Head.QuerySelector("title").InnerHtml;
            var author = document.Body.QuerySelector(".story__user .user__info .user__nick").InnerHtml;
            var ratingStr = document.Body.QuerySelector(".story__rating-count").InnerHtml;
            var hasRating = int.TryParse(ratingStr, out var rating);
            var timestampStr = document.Body.QuerySelector("time.caption.story__datetime.hint").GetAttribute("datetime");
            var timestamp = DateTimeOffset.ParseExact(timestampStr, "yyyy-MM-dd'T'HH:mm:sszzz", null);
            var totalCommentsCountStr = document.Body.QuerySelector("section.section_header h4#comments").GetAttribute("data-total");
            int.TryParse(totalCommentsCountStr, out var totalCommentsCount);
            var comments = document.Body.QuerySelectorAll("div.comments__container div.comment");
            foreach (var comment in comments)
            {
                storyComments.Add(ParseComment(comment));
            }
            
            return (storyTitle, author, hasRating ? rating : (int?) null, timestamp, totalCommentsCount);
        }

        private StoryComment ParseComment(IElement comment)
        {
            var commentIdStr = comment.GetAttribute("data-id");
            long.TryParse(commentIdStr, out var commentId);
            var metadataString = comment.GetAttribute("data-meta");
            var metadata = metadataString
                .Split(',')
                .Select(s => s.Split('='))
                .Select(arr => new {key = arr[0], value = arr.Length == 2 ? arr[1] : null})
                .ToDictionary(arg => arg.key, arg => arg.value);

            var parentId = long.Parse(metadata["pid"]);
            var timestamp = DateTimeOffset.ParseExact(metadata["d"], "yyyy-MM-dd'T'HH:mm:sszzz", null);

            var commentHeader = comment.QuerySelector("div.comment__body div.comment__header");
            var commentContentNode = comment.QuerySelector("div.comment__body div.comment__content");

            var userNode = commentHeader.QuerySelector("div.comment__user");
            var user = userNode.GetAttribute("data-name");
            var userAvatarUrl = userNode.QuerySelector("img")?.GetAttribute("src");
            var ratingNode = commentHeader.QuerySelector(".comment__rating-count");
            var ratingStr = ratingNode.HasTextNodes() ? ratingNode.InnerHtml : null;
            var rating = ratingStr != null ? (int?) int.Parse(ratingStr) : null;
            return new StoryComment(user, userAvatarUrl, commentId, parentId, rating, timestamp, commentContentNode.InnerHtml.Trim(' ', '\t', '\n'));
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private string CreateUri(string path)
        {
            return PikabuUri + path;
        }
    }

    internal class StoryComment
    {
        public string User { get; }
        public string UserAvatarUrl { get; }
        public long CommentId { get; }
        public long ParentId { get; }
        public int? Rating { get; }
        public DateTimeOffset Timestamp { get; }
        public string Body { get; }

        public StoryComment(string user, string userAvatarUrl, long commentId, long parentId, int? rating, DateTimeOffset timestamp, string body)
        {
            User = user;
            UserAvatarUrl = userAvatarUrl;
            CommentId = commentId;
            ParentId = parentId;
            Rating = rating;
            Timestamp = timestamp;
            Body = body;
        }
    }

    internal class StoryComments
    {
        public int StoryId { get; }
        public string Author { get; }
        public string StoryTitle { get; }
        public int? Rating { get; }
        public DateTimeOffset Timestamp { get; }
        public IReadOnlyList<StoryComment> Comments { get; }

        public StoryComments(int storyId, string author, string storyTitle, int? rating, DateTimeOffset timestamp, IReadOnlyList<StoryComment> comments)
        {
            StoryId = storyId;
            Author = author;
            StoryTitle = storyTitle;
            Rating = rating;
            Timestamp = timestamp;
            Comments = comments;
        }
    }
}
