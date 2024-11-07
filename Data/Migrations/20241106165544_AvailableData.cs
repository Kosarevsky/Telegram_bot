using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Data.Migrations
{
    /// <inheritdoc />
    public partial class AvailableData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AvailableDate_Execution_OperationId",
                table: "AvailableDate");

            migrationBuilder.DropTable(
                name: "UserSubscriptionItems");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "AvailableDate");

            migrationBuilder.RenameColumn(
                name: "OperationId",
                table: "AvailableDate",
                newName: "ExecutionId");

            migrationBuilder.RenameIndex(
                name: "IX_AvailableDate_OperationId",
                table: "AvailableDate",
                newName: "IX_AvailableDate_ExecutionId");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Execution",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_AvailableDate_Execution_ExecutionId",
                table: "AvailableDate",
                column: "ExecutionId",
                principalTable: "Execution",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AvailableDate_Execution_ExecutionId",
                table: "AvailableDate");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Execution");

            migrationBuilder.RenameColumn(
                name: "ExecutionId",
                table: "AvailableDate",
                newName: "OperationId");

            migrationBuilder.RenameIndex(
                name: "IX_AvailableDate_ExecutionId",
                table: "AvailableDate",
                newName: "IX_AvailableDate_OperationId");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "AvailableDate",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "UserSubscriptionItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserSubscriptionId = table.Column<int>(type: "int", nullable: false),
                    AvailableDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSubscriptionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSubscriptionItems_UserSubscription_UserSubscriptionId",
                        column: x => x.UserSubscriptionId,
                        principalTable: "UserSubscription",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSubscriptionItems_UserSubscriptionId",
                table: "UserSubscriptionItems",
                column: "UserSubscriptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AvailableDate_Execution_OperationId",
                table: "AvailableDate",
                column: "OperationId",
                principalTable: "Execution",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
