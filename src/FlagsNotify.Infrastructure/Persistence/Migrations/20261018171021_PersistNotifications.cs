using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlagsNotify.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PersistNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    delivery_completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    event_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    recipient_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sender_user_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    sender_user_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    event_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    body = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    route = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_event_id",
                table: "notifications",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_notifications_recipient_user_id_occurred_at_id",
                table: "notifications",
                columns: new[] { "recipient_user_id", "occurred_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_notifications_recipient_user_id_read_at",
                table: "notifications",
                columns: new[] { "recipient_user_id", "read_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notifications");
        }
    }
}
