using Microsoft.EntityFrameworkCore.Migrations;
using MySql.EntityFrameworkCore.Metadata;

#nullable disable

namespace Ordenes.Api.Migrations
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
                name: "ORD_Cat_EstadoOrden",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    Nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ORD_Cat_EstadoOrden", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ORD_Orden",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    IdDireccionEnvio = table.Column<int>(type: "int", nullable: false),
                    IdEstadoOrden = table.Column<int>(type: "int", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DescuentoAplicado = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TotalFinal = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ClaveIdempotencia = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ORD_Orden", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ORD_Orden_ORD_Cat_EstadoOrden_IdEstadoOrden",
                        column: x => x.IdEstadoOrden,
                        principalTable: "ORD_Cat_EstadoOrden",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ORD_Detalle",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySQL:ValueGenerationStrategy", MySQLValueGenerationStrategy.IdentityColumn),
                    IdOrden = table.Column<int>(type: "int", nullable: false),
                    IdProducto = table.Column<int>(type: "int", nullable: false),
                    NombreProducto = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false),
                    EsElectronica = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DescuentoLinea = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ORD_Detalle", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ORD_Detalle_ORD_Orden_IdOrden",
                        column: x => x.IdOrden,
                        principalTable: "ORD_Orden",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ORD_Detalle_IdOrden",
                table: "ORD_Detalle",
                column: "IdOrden");

            migrationBuilder.CreateIndex(
                name: "IX_ORD_Orden_ClaveIdempotencia",
                table: "ORD_Orden",
                column: "ClaveIdempotencia",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ORD_Orden_IdEstadoOrden",
                table: "ORD_Orden",
                column: "IdEstadoOrden");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ORD_Detalle");

            migrationBuilder.DropTable(
                name: "ORD_Orden");

            migrationBuilder.DropTable(
                name: "ORD_Cat_EstadoOrden");
        }
    }
}
