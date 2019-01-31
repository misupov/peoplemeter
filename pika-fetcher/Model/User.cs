using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PikaFetcher.Model
{
    public class User
    {
        [Key]
        public string UserName { get; set; }

        public ICollection<Comment> Comments { get; set; }
    }
}