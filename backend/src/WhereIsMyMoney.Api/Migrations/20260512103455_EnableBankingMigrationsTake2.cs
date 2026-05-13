using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhereIsMyMoney.Api.Migrations
{
    /// <inheritdoc />
    public partial class EnableBankingMigrationsTake2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Configuration",
                table: "EnableBanking",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Configuration",
                table: "EnableBanking");
        }
    }
}
