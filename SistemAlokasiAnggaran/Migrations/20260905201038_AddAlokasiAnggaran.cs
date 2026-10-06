using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SistemAlokasiAnggaran.Migrations
{
    /// <inheritdoc />
    public partial class AddAlokasiAnggaran : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "alokasi_anggaran",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Periode = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TotalDana = table.Column<decimal>(type: "REAL", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alokasi_anggaran", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "program_anggaran",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlokasiAnggaranId = table.Column<int>(type: "INTEGER", nullable: false),
                    NamaProgram = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AnggaranDiajukan = table.Column<decimal>(type: "REAL", nullable: true),
                    AnggaranMinimum = table.Column<decimal>(type: "REAL", nullable: true),
                    AnggaranMaksimum = table.Column<decimal>(type: "REAL", nullable: true),
                    TingkatPrioritas = table.Column<int>(type: "INTEGER", nullable: false),
                    EstimasiManfaat = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_anggaran", x => x.Id);
                    table.ForeignKey(
                        name: "FK_program_anggaran_alokasi_anggaran_AlokasiAnggaranId",
                        column: x => x.AlokasiAnggaranId,
                        principalTable: "alokasi_anggaran",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "hasil_alokasi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProgramAnggaranId = table.Column<int>(type: "INTEGER", nullable: false),
                    AlokasiOptimal = table.Column<decimal>(type: "REAL", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<int>(type: "INTEGER", nullable: false),
                    UpdatedBy = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_hasil_alokasi", x => x.Id);
                    table.ForeignKey(
                        name: "FK_hasil_alokasi_program_anggaran_ProgramAnggaranId",
                        column: x => x.ProgramAnggaranId,
                        principalTable: "program_anggaran",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Deskripsi", "NamaRole" },
                values: new object[] { 3, "Manajer Perencanaan dengan akses alokasi anggaran", "Manajer Perencanaan" });

            migrationBuilder.InsertData(
                table: "Menu_Roles",
                columns: new[] { "Id", "Icon", "NamaMenu", "RoleId", "Url", "Urutan" },
                values: new object[,]
                {
                    { 4, "fas fa-home", "Halaman Depan", 3, "/Home/Index", 1 },
                    { 5, "fas fa-money-bill-wave", "Alokasi Anggaran", 3, "/AlokasiAnggaran/Index", 2 },
                    { 6, "fas fa-history", "History Anggaran", 3, "/AlokasiAnggaran/History", 3 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_hasil_alokasi_ProgramAnggaranId",
                table: "hasil_alokasi",
                column: "ProgramAnggaranId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_program_anggaran_AlokasiAnggaranId",
                table: "program_anggaran",
                column: "AlokasiAnggaranId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "hasil_alokasi");

            migrationBuilder.DropTable(
                name: "program_anggaran");

            migrationBuilder.DropTable(
                name: "alokasi_anggaran");

            migrationBuilder.DeleteData(
                table: "Menu_Roles",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Menu_Roles",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Menu_Roles",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3);
        }
    }
}
