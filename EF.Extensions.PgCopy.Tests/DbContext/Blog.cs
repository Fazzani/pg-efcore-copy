using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EF.Extensions.PgCopy.Tests.DbContext
{
    [Table("blog", Schema = "public")]
    public class Blog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("url")] public string Url { get; set; }

        [Column("creation_datetime", TypeName = "timestamptz")]
        public DateTime CreationDateTime { get; set; } = DateTime.UtcNow;
        
        public List<Post> Posts { get; set; }
    }
}