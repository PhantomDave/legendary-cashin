using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WhereIsMyMoney.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRulesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Rules",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MatchType = table.Column<int>(type: "integer", nullable: false),
                    DescriptionPattern = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    MinAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MaxAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    BudgetId = table.Column<long>(type: "bigint", nullable: true),
                    DaysOfWeek = table.Column<int[]>(type: "integer[]", nullable: true),
                    DayOfMonth = table.Column<int>(type: "integer", nullable: true),
                    CategoryIds = table.Column<int[]>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rules_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Rules_Budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalTable: "Budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Rules_AccountId_IsActive_Priority",
                table: "Rules",
                columns: new[] { "AccountId", "IsActive", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_Rules_BudgetId",
                table: "Rules",
                column: "BudgetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Rules");
        }
    }
}
