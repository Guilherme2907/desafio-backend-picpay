using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PipcPaySimplified.Infra.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVersionColumnToEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RowVersion",
                table: "Users",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RowVersion",
                table: "Accounts",
                type: "timestamp(6)",
                rowVersion: true,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Accounts");
        }
    }
}
