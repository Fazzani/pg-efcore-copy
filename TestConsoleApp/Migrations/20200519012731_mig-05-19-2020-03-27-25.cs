using Microsoft.EntityFrameworkCore.Migrations;

namespace TestConsoleApp.Migrations
{
    public partial class mig05192020032725 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_blog_blog_id",
                table: "Posts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Posts",
                table: "Posts");

            migrationBuilder.RenameTable(
                name: "Posts",
                newName: "post",
                newSchema: "public");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_blog_id",
                schema: "public",
                table: "post",
                newName: "IX_post_blog_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_post",
                schema: "public",
                table: "post",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_post_blog_blog_id",
                schema: "public",
                table: "post",
                column: "blog_id",
                principalSchema: "public",
                principalTable: "blog",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_post_blog_blog_id",
                schema: "public",
                table: "post");

            migrationBuilder.DropPrimaryKey(
                name: "PK_post",
                schema: "public",
                table: "post");

            migrationBuilder.RenameTable(
                name: "post",
                schema: "public",
                newName: "Posts");

            migrationBuilder.RenameIndex(
                name: "IX_post_blog_id",
                table: "Posts",
                newName: "IX_Posts_blog_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Posts",
                table: "Posts",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_blog_blog_id",
                table: "Posts",
                column: "blog_id",
                principalSchema: "public",
                principalTable: "blog",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
