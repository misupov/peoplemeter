using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PikaModel;
using PikaModel.Model;

namespace PikaFetcher
{
    internal abstract class AbstractFetcher
    {
        protected PikabuApi Api { get; set; }

        protected AbstractFetcher(PikabuApi api)
        {
            Api = api;
        }

        public abstract Task FetchLoop();

        protected async Task<Task> ProcessStory(int storyId, char fetcher, CancellationToken cancellationToken)
        {
            var storyComments = await Api.GetStoryComments(storyId, cancellationToken);
            return Task.Run(() => SaveStory(storyComments, fetcher, cancellationToken), cancellationToken);
        }

        private async Task SaveStory(StoryComments storyComments, char fetcher, CancellationToken cancellationToken)
        {
            using (var db = new PikabuContext())
            using (var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken))
            {
                var scanTime = DateTime.UtcNow;
                var story = await db.Stories.SingleOrDefaultAsync(s => s.StoryId == storyComments.StoryId, cancellationToken);
                if (story == null)
                {
                    story = new Story
                    {
                        StoryId = storyComments.StoryId,
                        Rating = storyComments.Rating,
                        Title = storyComments.StoryTitle,
                        Author = storyComments.Author,
                        DateTimeUtc = storyComments.Timestamp.UtcDateTime,
                        Comments = new List<Comment>()
                    };
                    await db.Stories.AddAsync(story, cancellationToken);
                }

                story.Rating = storyComments.Rating;
                story.Title = storyComments.StoryTitle;
                story.Author = storyComments.Author;
                story.LastScanUtc = scanTime;

                var storyCommentIds = storyComments.Comments.Select(c => c.CommentId).ToArray();
                var existingComments = await db.Comments.Where(c => storyCommentIds.Contains(c.CommentId)).ToDictionaryAsync(c => c.CommentId, cancellationToken);

                var storyUserNames = new HashSet<string>(storyComments.Comments.Select(c => c.User));
                var existingUsers = (await db.Users.Where(c => storyUserNames.Contains(c.UserName)).ToDictionaryAsync(u => u.UserName, cancellationToken));

                foreach (var comment in storyComments.Comments)
                {
                    if (!existingUsers.TryGetValue(comment.User, out var user))
                    {
                        user = new User { UserName = comment.User, AvatarUrl = comment.UserAvatarUrl, Comments = new List<Comment>() };
                        await db.Users.AddAsync(user, cancellationToken);
                        existingUsers[user.UserName] = user;
                    }
                    else
                    {
                        user.AvatarUrl = comment.UserAvatarUrl;
                    }

                    if (!existingComments.TryGetValue(comment.CommentId, out var c))
                    {
                        var item = new Comment
                        {
                            CommentId = comment.CommentId,
                            ParentId = comment.ParentId,
                            DateTimeUtc = comment.Timestamp.UtcDateTime,
                            Rating = comment.Rating,
                            Story = story,
                            UserName = comment.User,
                            CommentContent = new CommentContent { BodyHtml = comment.Body }
                        };
                        await db.Comments.AddAsync(item, cancellationToken);
                        existingComments[item.CommentId] = item;
                    }
                    else
                    {
                        c.Rating = comment.Rating;
                    }
                }

                Console.WriteLine($"{fetcher}{DateTime.UtcNow} ({storyComments.StoryId}) {storyComments.Rating?.ToString("+0;-#") ?? "?"} {storyComments.StoryTitle}");

                await db.SaveChangesAsync(cancellationToken);

                transaction.Commit();
            }
        }

    }
}