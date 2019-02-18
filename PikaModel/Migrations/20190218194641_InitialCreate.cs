using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace PikaModel.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommentContents",
                columns: table => new
                {
                    CommentContentId = table.Column<long>(type: "bigint(20)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BodyHtml = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.CommentContentId);
                });

            migrationBuilder.CreateTable(
                name: "FetcherStats",
                columns: table => new
                {
                    FetcherName = table.Column<string>(type: "varchar(255)", nullable: false),
                    StoriesPerSecondForLastHour = table.Column<double>(nullable: false),
                    StoriesPerSecondForLastMinute = table.Column<double>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.FetcherName);
                });

            migrationBuilder.CreateTable(
                name: "Stories",
                columns: table => new
                {
                    StoryId = table.Column<int>(type: "int(11)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Title = table.Column<string>(type: "longtext", nullable: true),
                    Rating = table.Column<int>(type: "int(11)", nullable: true),
                    DateTimeUtc = table.Column<DateTime>(nullable: false),
                    LastScanUtc = table.Column<DateTime>(nullable: false),
                    Author = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.StoryId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserName = table.Column<string>(type: "varchar(100)", nullable: false),
                    AvatarUrl = table.Column<string>(type: "longtext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.UserName);
                });

            migrationBuilder.CreateTable(
                name: "Comments",
                columns: table => new
                {
                    CommentId = table.Column<long>(type: "bigint(20)", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ParentId = table.Column<long>(type: "bigint(20)", nullable: false),
                    StoryId = table.Column<int>(type: "int(11)", nullable: false),
                    Rating = table.Column<int>(type: "int(11)", nullable: true),
                    UserName = table.Column<string>(type: "varchar(100)", nullable: true),
                    DateTimeUtc = table.Column<DateTime>(nullable: false),
                    CommentContentId = table.Column<long>(type: "bigint(20)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PRIMARY", x => x.CommentId);
                    table.ForeignKey(
                        name: "FK_Comments_CommentContents_CommentContentId",
                        column: x => x.CommentContentId,
                        principalTable: "CommentContents",
                        principalColumn: "CommentContentId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Comments_Stories_StoryId",
                        column: x => x.StoryId,
                        principalTable: "Stories",
                        principalColumn: "StoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Comments_Users_UserName",
                        column: x => x.UserName,
                        principalTable: "Users",
                        principalColumn: "UserName",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Comments_CommentContentId",
                table: "Comments",
                column: "CommentContentId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_DateTimeUtc",
                table: "Comments",
                column: "DateTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_StoryId",
                table: "Comments",
                column: "StoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Comments_UserName",
                table: "Comments",
                column: "UserName");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Comments");

            migrationBuilder.DropTable(
                name: "FetcherStats");

            migrationBuilder.DropTable(
                name: "CommentContents");

            migrationBuilder.DropTable(
                name: "Stories");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
