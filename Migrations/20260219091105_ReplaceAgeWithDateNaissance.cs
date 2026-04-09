using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace seragenda.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAgeWithDateNaissance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "age",
                table: "utilisateur");

            migrationBuilder.AddColumn<DateOnly>(
                name: "date_naissance",
                table: "utilisateur",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "date_naissance",
                table: "utilisateur");

            migrationBuilder.AddColumn<int>(
                name: "age",
                table: "utilisateur",
                type: "integer",
                nullable: true);
        }
    }
}
