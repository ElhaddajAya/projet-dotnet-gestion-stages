using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionStages.Migrations
{
    /// <inheritdoc />
    public partial class AjouterChampCV : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CheminCV",
                table: "Candidatures",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheminCV",
                table: "Candidatures");
        }
    }
}
