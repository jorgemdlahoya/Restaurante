using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurante.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Categorias : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categorias",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    nombre_normalizado = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    nombre_corto = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    nombre_corto_normalizado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false),
                    creada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    eliminada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.id);
                    table.CheckConstraint("ck_categorias_orden", "orden >= 1");
                    table.ForeignKey(
                        name: "FK_categorias_restaurantes_restaurante_id",
                        column: x => x.restaurante_id,
                        principalTable: "restaurantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_categorias_restaurante_nombre",
                table: "categorias",
                columns: new[] { "restaurante_id", "nombre_normalizado" },
                unique: true,
                filter: "eliminada_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_categorias_restaurante_nombre_corto",
                table: "categorias",
                columns: new[] { "restaurante_id", "nombre_corto_normalizado" },
                unique: true,
                filter: "eliminada_en IS NULL AND nombre_corto IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_categorias_restaurante_orden_nombre",
                table: "categorias",
                columns: new[] { "restaurante_id", "orden", "nombre" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categorias");
        }
    }
}
