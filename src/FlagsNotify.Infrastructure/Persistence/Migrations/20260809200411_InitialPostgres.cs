using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlagsNotify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_push_devices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    expo_push_token = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    registered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_push_devices", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_push_devices_expo_push_token",
                table: "user_push_devices",
                column: "expo_push_token",
                unique: true);  

            migrationBuilder.CreateIndex(
                name: "IX_user_push_devices_user_id_is_active",
                table: "user_push_devices",
                columns: new[] { "user_id", "is_active" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_push_devices");
        }
    }
}
