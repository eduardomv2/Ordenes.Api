using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Pagos.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PAG_MetodoGuardado",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    TokenIdOpenPay = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Ultimos4Digitos = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: false),
                    MarcaTarjeta = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    EsCreditoTienda = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    EsPrincipal = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PAG_MetodoGuardado", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PAG_CargoRecurrente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    IdMetodoPago = table.Column<int>(type: "int", nullable: false),
                    MontoMensual = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DiaCobro = table.Column<int>(type: "int", nullable: false),
                    Activo = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    UltimoCobro = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ProximoCobro = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PAG_CargoRecurrente", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PAG_CargoRecurrente_PAG_MetodoGuardado_IdMetodoPago",
                        column: x => x.IdMetodoPago,
                        principalTable: "PAG_MetodoGuardado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PAG_Transaccion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    IdOrden = table.Column<int>(type: "int", nullable: false),
                    IdMetodoPago = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    MesesSinIntereses = table.Column<int>(type: "int", nullable: false),
                    IdTransaccionOpenPay = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true),
                    EstadoPago = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                    EsCargoRecurrente = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PAG_Transaccion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PAG_Transaccion_PAG_MetodoGuardado_IdMetodoPago",
                        column: x => x.IdMetodoPago,
                        principalTable: "PAG_MetodoGuardado",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_PAG_CargoRecurrente_IdMetodoPago",
                table: "PAG_CargoRecurrente",
                column: "IdMetodoPago");

            migrationBuilder.CreateIndex(
                name: "IX_PAG_Transaccion_IdMetodoPago",
                table: "PAG_Transaccion",
                column: "IdMetodoPago");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PAG_CargoRecurrente");

            migrationBuilder.DropTable(
                name: "PAG_Transaccion");

            migrationBuilder.DropTable(
                name: "PAG_MetodoGuardado");
        }
    }
}
