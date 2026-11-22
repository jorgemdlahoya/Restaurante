using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurante.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Restaurantes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "restaurantes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    color_acento = table.Column<string>(type: "char(7)", nullable: true),
                    color_fondo = table.Column<string>(type: "char(7)", nullable: true),
                    color_oscuro = table.Column<string>(type: "char(7)", nullable: true),
                    color_principal = table.Column<string>(type: "char(7)", nullable: true),
                    color_texto = table.Column<string>(type: "char(7)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restaurantes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "restaurantes_logo",
                columns: table => new
                {
                    restaurante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contenido = table.Column<byte[]>(type: "bytea", nullable: false),
                    tipo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    etag = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_restaurantes_logo", x => x.restaurante_id);
                    table.ForeignKey(
                        name: "FK_restaurantes_logo_restaurantes_restaurante_id",
                        column: x => x.restaurante_id,
                        principalTable: "restaurantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_restaurantes_nombre",
                table: "restaurantes",
                column: "nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_restaurantes_slug",
                table: "restaurantes",
                column: "slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "restaurantes_logo");

            migrationBuilder.DropTable(
                name: "restaurantes");
        }
    }
}
