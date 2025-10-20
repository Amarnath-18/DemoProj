using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demoProject.Migrations
{
    /// <inheritdoc />
    public partial class addedondeletecascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Users_SenderId",
                table: "Shipments");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_Users_SenderId",
                table: "Shipments",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipments_Users_SenderId",
                table: "Shipments");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipments_Users_SenderId",
                table: "Shipments",
                column: "SenderId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
