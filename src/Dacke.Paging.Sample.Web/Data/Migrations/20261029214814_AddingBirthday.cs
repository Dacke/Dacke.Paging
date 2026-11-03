using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EFCorePaging.Sample.Web.Data.Migrations 
{
    /// <inheritdoc />
    public partial class AddingBirthday : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BirthDay",
                table: "People",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql(
                @"UPDATE [People]
                  SET [BirthDay] = DATEADD(year, -[Age], GETDATE())
                  WHERE [Age] IS NOT NULL AND [Age] > 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BirthDay",
                table: "People");
        }
    }
}
