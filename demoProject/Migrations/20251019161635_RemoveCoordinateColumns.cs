using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace demoProject.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCoordinateColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "TrackingUpdates");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "TrackingUpdates");

            migrationBuilder.DropColumn(
                name: "DestinationLatitude",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DestinationLongitude",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "OriginLatitude",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "OriginLongitude",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "CurrentLatitude",
                table: "Drivers");

            migrationBuilder.DropColumn(
                name: "CurrentLongitude",
                table: "Drivers");

            migrationBuilder.AddColumn<string>(
                name: "DestinationCity",
                table: "Shipments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DestinationRegion",
                table: "Shipments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginCity",
                table: "Shipments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginRegion",
                table: "Shipments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DestinationCity",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "DestinationRegion",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "OriginCity",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "OriginRegion",
                table: "Shipments");

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "TrackingUpdates",
                type: "numeric(10,8)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "TrackingUpdates",
                type: "numeric(11,8)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DestinationLatitude",
                table: "Shipments",
                type: "numeric(10,8)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DestinationLongitude",
                table: "Shipments",
                type: "numeric(11,8)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginLatitude",
                table: "Shipments",
                type: "numeric(10,8)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginLongitude",
                table: "Shipments",
                type: "numeric(11,8)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentLatitude",
                table: "Drivers",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentLongitude",
                table: "Drivers",
                type: "numeric",
                nullable: true);
        }
    }
}
