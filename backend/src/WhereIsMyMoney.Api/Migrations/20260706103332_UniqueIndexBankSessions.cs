using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WhereIsMyMoney.Api.Migrations
{
    /// <inheritdoc />
    public partial class UniqueIndexBankSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EnableBankingSessions_AccountId_IntegrationId_AspspName_AspspCountry",
                table: "EnableBankingSessions",
                columns: new[] { "AccountId", "IntegrationId", "AspspName", "AspspCountry" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EnableBankingSessions_AccountId_IntegrationId_AspspName_AspspCountry",
                table: "EnableBankingSessions");
        }
    }
}
