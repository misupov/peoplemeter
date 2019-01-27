using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Newtonsoft.Json;

namespace PikaFetcher
{
    internal class PikabuApi : IDisposable
    {
        private const string PikabuUri = "https://pikabu.ru";

        private volatile HttpClient _httpClient;
        private readonly string _proxy;

        public PikabuApi(string proxy)
        {
            _proxy = proxy;
        }

        public async Task Init()
        {
            if (_httpClient == null)
            {
                var cookieContainer = new CookieContainer();
                _httpClient = new HttpClient(new HttpClientHandler
                {
                    CookieContainer = cookieContainer,
                    Proxy = new WebProxy(_proxy)
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

            var doc = new HtmlDocument();
            doc.LoadHtml(await _httpClient.GetStringAsync(CreateUri("/new")));
            var xPathNavigator = doc.CreateNavigator();
            var latestStoryIdStr = xPathNavigator.SelectSingleNode("//*/article[1]").GetAttribute("data-story-id", null);
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
            var (storyTitle, rating, timestamp, totalCommentsCount) = await LoadRootComments(storyId, comments);

            while (totalCommentsCount > comments.Count)
            {
                await LoadComments(storyId, comments.Last().CommentId, comments);
            }

            return new StoryComments(storyId, storyTitle, rating, timestamp, comments);
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
            try
            {
                var responseObject = (dynamic) JsonConvert.DeserializeObject(response);
                foreach (var comment in responseObject.data.comments)
                {
                    var doc = new HtmlDocument();
                    doc.LoadHtml((string) comment.html);
                    var storyComments = ParseRootComment((HtmlNodeNavigator) doc.CreateNavigator().SelectSingleNode("div[@class='comment']"));
                    foreach (var storyComment in storyComments)
                    {
                        comments.Add(storyComment);
                    }
                }
            }
            catch
            {

            }
        }

        private async Task<(string storyTitle, int? rating, DateTimeOffset timestamp, int totalCommentsCount)> LoadRootComments(int storyId, List<StoryComment> comments)
        {
            var doc = new HtmlDocument();
            var html = await _httpClient.GetStringAsync(CreateUri("/story/_" + storyId));
            doc.LoadHtml(html);

            var navigator = doc.CreateNavigator();
            var storyTitle = navigator.SelectSingleNode("/html/head/title").Value;
            var ratingStr = navigator.SelectSingleNode("//*/div[@class='story__rating-count']").Value;
            var hasRating = int.TryParse(ratingStr, out var rating);
            var timestampStr = navigator.SelectSingleNode("//*/time[@class='caption story__datetime hint']/@datetime").Value;
            var timestamp = DateTimeOffset.ParseExact(timestampStr, "yyyy-MM-dd'T'HH:mm:sszzz", null);
            var totalCommentsCount = navigator.SelectSingleNode("//*/section[@class='section_header']/h4[@id='comments']/@data-total").ValueAsInt;
            var iterator = navigator.Select("//*/div[@class='comments__container']/div[@class='comment']");
            foreach (HtmlNodeNavigator rootComment in iterator)
            {
                foreach (var comment in ParseRootComment(rootComment))
                {
                    comments.Add(comment);
                }
            }

            return (storyTitle, hasRating ? rating : (int?) null, timestamp, totalCommentsCount);
        }

        private IEnumerable<StoryComment> ParseRootComment(HtmlNodeNavigator rootComment)
        {
            yield return ParseComment(rootComment);
            foreach (var comment in rootComment.Select(".//*/div[@class='comment']").OfType<HtmlNodeNavigator>())
            {
                yield return ParseComment(comment);
            }
        }

        private StoryComment ParseComment(HtmlNodeNavigator comment)
        {
            var commentId = comment.SelectSingleNode("@data-id").ValueAsLong;
            var metadataString = comment.SelectSingleNode("@data-meta").Value;
            var metadata = metadataString
                .Split(',')
                .Select(s => s.Split('='))
                .Select(arr => new {key = arr[0], value = arr.Length == 2 ? arr[1] : null})
                .ToDictionary(arg => arg.key, arg => arg.value);

            var parentId = long.Parse(metadata["pid"]);
            var timestamp = DateTimeOffset.ParseExact(metadata["d"], "yyyy-MM-dd'T'HH:mm:sszzz", null);

            var commentHeader = comment.SelectSingleNode("div[contains(@class, 'comment__body')]/div[contains(@class, 'comment__header')]");
            var commentContent = comment.SelectSingleNode("div[contains(@class, 'comment__body')]/div[contains(@class, 'comment__content')]");
            var user = commentHeader.SelectSingleNode("div[contains(@class, 'comment__user')]/@data-name").Value;
            return new StoryComment(user, commentId, parentId, timestamp, commentContent.ToString());
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private Uri CreateUri(string path)
        {
            return new Uri(PikabuUri + path);
        }
    }

    internal class StoryComment
    {
        public string User { get; }
        public long CommentId { get; }
        public long ParentId { get; }
        public DateTimeOffset Timestamp { get; }
        public string Body { get; }

        public StoryComment(string user, long commentId, long parentId, DateTimeOffset timestamp, string body)
        {
            User = user;
            CommentId = commentId;
            ParentId = parentId;
            Timestamp = timestamp;
            Body = body;
        }
    }

    internal class StoryComments
    {
        public int StoryId { get; }
        public string StoryTitle { get; }
        public int? Rating { get; }
        public DateTimeOffset Timestamp { get; }
        public IReadOnlyList<StoryComment> Comments { get; }

        public StoryComments(int storyId, string storyTitle, int? rating, DateTimeOffset timestamp, IReadOnlyList<StoryComment> comments)
        {
            StoryId = storyId;
            StoryTitle = storyTitle;
            Rating = rating;
            Timestamp = timestamp;
            Comments = comments;
        }
    }
}
