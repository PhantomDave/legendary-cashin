using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using WhereIsMyMoney.Api.Data;

#nullable disable

namespace WhereIsMyMoney.Api.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(AppDbContext))]
    [Migration("20260513132000_AddExternalRefToTransactions")]
    public partial class AddExternalRefToTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "ALTER TABLE \"Transactions\" ADD COLUMN IF NOT EXISTS \"ExternalRef\" character varying(128);");

            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX IF NOT EXISTS \"IX_Transactions_AccountId_ExternalRef\" " +
                "ON \"Transactions\" (\"AccountId\", \"ExternalRef\") " +
                "WHERE \"ExternalRef\" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_Transactions_AccountId_ExternalRef\";");
            migrationBuilder.Sql("ALTER TABLE \"Transactions\" DROP COLUMN IF EXISTS \"ExternalRef\";");
        }
    }
}
