using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GestionStages.Migrations
{
    /// <inheritdoc />
    public partial class AjouterCheminFichierPDFConvention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CheminFichierPDF",
                table: "Conventions",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheminFichierPDF",
                table: "Conventions");
        }
    }
}
