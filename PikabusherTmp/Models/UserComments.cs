//using System.Collections.Generic;
//using MongoDB.Bson;
//using MongoDB.Bson.Serialization.Attributes;
//
//namespace PikabusherTmp.Models
//{
//    public class UserComments
//    {
//        public ObjectId Id { get; set; }
//
//        [BsonElement("UserName")]
//        public string UserName { get; set; }
//
//        [BsonElement("Comments")]
//        public IList<UserComment> Comments { get; set; }
//    }
//
//    public class UserComment
//    {
//        [BsonElement("Timestamp")]
//        public BsonDateTime Timestamp { get; set; }
//
//        [BsonElement("StoryId")]
//        public int StoryId { get; set; }
//
//        [BsonElement("CommentId")]
//        public long CommentId { get; set; }
//    }
//}