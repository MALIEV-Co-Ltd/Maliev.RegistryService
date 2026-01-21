using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Maliev.RegistryService.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,");

            migrationBuilder.CreateTable(
                name: "ThaiLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    SubDistrictTh = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DistrictTh = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProvinceTh = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SubDistrictEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DistrictEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ProvinceEn = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThaiLocations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ThaiLocations_DistrictEn",
                table: "ThaiLocations",
                column: "DistrictEn")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_ThaiLocations_DistrictTh",
                table: "ThaiLocations",
                column: "DistrictTh")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_ThaiLocations_PostalCode",
                table: "ThaiLocations",
                column: "PostalCode");

            migrationBuilder.CreateIndex(
                name: "IX_ThaiLocations_ProvinceEn",
                table: "ThaiLocations",
                column: "ProvinceEn")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_ThaiLocations_ProvinceTh",
                table: "ThaiLocations",
                column: "ProvinceTh")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_ThaiLocations_SubDistrictEn",
                table: "ThaiLocations",
                column: "SubDistrictEn")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_ThaiLocations_SubDistrictTh",
                table: "ThaiLocations",
                column: "SubDistrictTh")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ThaiLocations");
        }
    }
}
