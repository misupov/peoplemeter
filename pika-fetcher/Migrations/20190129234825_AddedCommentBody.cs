using Microsoft.EntityFrameworkCore.Migrations;

namespace PikaFetcher.Migrations
{
    public partial class AddedCommentBody : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CommentBody",
                table: "Comments",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CommentBody",
                table: "Comments");
        }
    }
}
