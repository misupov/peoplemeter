using System;
using System.Collections.Generic;

namespace PikaModel.Models
{
    public partial class User
    {
        public User()
        {
            Comments = new HashSet<Comment>();
        }

        public string UserName { get; set; }
        public string AvatarUrl { get; set; }

        public virtual ICollection<Comment> Comments { get; set; }
    }
}
