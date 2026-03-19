using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NutritionMonitor.DAL.Migrations
{
    /// <inheritdoc />
    public partial class SeedAdminHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$mytc0Ge2txX31qR05Opu6.I0.gALwl14AmJKbVJR0uauQukjLowbC");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$5Y3FjyQlQ1v6xITbkwqaAOyOb8q6mT4rjV5ABCDE1234567890abcd");
        }
    }
}
