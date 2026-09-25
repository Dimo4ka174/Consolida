using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace DB.Migrations
{
    /// <inheritdoc />
    public partial class AddConsolidationAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConsolidationPoolId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedAt",
                table: "Orders",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LockedByUserId",
                table: "Orders",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConsolidationPoolHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PoolId = table.Column<int>(type: "integer", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "varchar(500)", nullable: false),
                    OldValue = table.Column<string>(type: "varchar(200)", nullable: true),
                    NewValue = table.Column<string>(type: "varchar(200)", nullable: true),
                    ChangedBy = table.Column<string>(type: "varchar(200)", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidationPoolHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsolidationPools",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LockedByUserId = table.Column<string>(type: "text", nullable: true),
                    TargetWeek = table.Column<int>(type: "integer", nullable: false),
                    TotalWeight = table.Column<decimal>(type: "numeric", nullable: false),
                    Color = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ExpectedDeliveryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidationPools", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConsolidationWeightLimits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Value = table.Column<decimal>(type: "numeric", nullable: false),
                    IsSystem = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsolidationWeightLimits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrderNotifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OrderId = table.Column<int>(type: "integer", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderNotifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderNotifications_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ConsolidationPoolId",
                table: "Orders",
                column: "ConsolidationPoolId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationPoolHistories_PoolId",
                table: "ConsolidationPoolHistories",
                column: "PoolId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationPools_IsDeleted",
                table: "ConsolidationPools",
                column: "IsDeleted");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationPools_Status",
                table: "ConsolidationPools",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ConsolidationWeightLimits_Value_IsDeleted",
                table: "ConsolidationWeightLimits",
                columns: new[] { "Value", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_OrderNotifications_DueDate",
                table: "OrderNotifications",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_OrderNotifications_OrderId_IsCompleted_IsDeleted",
                table: "OrderNotifications",
                columns: new[] { "OrderId", "IsCompleted", "IsDeleted" });

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_ConsolidationPools_ConsolidationPoolId",
                table: "Orders",
                column: "ConsolidationPoolId",
                principalTable: "ConsolidationPools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_ConsolidationPools_ConsolidationPoolId",
                table: "Orders");

            migrationBuilder.DropTable(
                name: "ConsolidationPoolHistories");

            migrationBuilder.DropTable(
                name: "ConsolidationPools");

            migrationBuilder.DropTable(
                name: "ConsolidationWeightLimits");

            migrationBuilder.DropTable(
                name: "OrderNotifications");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ConsolidationPoolId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ConsolidationPoolId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "LockedAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "LockedByUserId",
                table: "Orders");
        }
    }
}
