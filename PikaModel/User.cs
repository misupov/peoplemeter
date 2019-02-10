using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PikaModel
{
    public class User
    {
        [Key]
        public string UserName { get; set; }

        public string AvatarUrl { get; set; }

        public ICollection<Comment> Comments { get; set; }
    }
}