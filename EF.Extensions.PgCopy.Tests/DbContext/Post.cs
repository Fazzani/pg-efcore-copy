using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EF.Extensions.PgCopy.Tests.DbContext
{
    [Table("post", Schema = "public")]
    public class Post
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }

        [Column("title")] public string Title { get; set; }
        [Column("content")] public string Content { get; set; }
        [Column("blog_id")] public int? BlogId { get; set; }
        [Column("post_date")] public DateTime PostDate { get; set; }
        [Column("online"), Required] public bool Online { get; set; }

        [Column("creation_datetime", TypeName = "timestamptz")]
        public DateTime CreationDateTime { get; set; } = DateTime.Now;

        public Blog Blog { get; set; }
    }
}