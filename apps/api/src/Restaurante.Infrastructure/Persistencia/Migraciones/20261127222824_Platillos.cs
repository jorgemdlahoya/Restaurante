using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurante.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Platillos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "platillos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    nombre_normalizado = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    nombre_corto = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    nombre_corto_normalizado = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    descripcion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    eliminada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ingredientes = table.Column<List<string>>(type: "text[]", nullable: false, defaultValueSql: "'{}'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platillos", x => x.id);
                    table.CheckConstraint("ck_platillos_orden", "orden >= 1");
                    table.ForeignKey(
                        name: "FK_platillos_categorias_categoria_id",
                        column: x => x.categoria_id,
                        principalTable: "categorias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_platillos_restaurantes_restaurante_id",
                        column: x => x.restaurante_id,
                        principalTable: "restaurantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_platillos_categoria_orden_nombre",
                table: "platillos",
                columns: new[] { "categoria_id", "orden", "nombre" });

            migrationBuilder.CreateIndex(
                name: "ix_platillos_restaurante_nombre",
                table: "platillos",
                columns: new[] { "restaurante_id", "nombre_normalizado" },
                unique: true,
                filter: "eliminada_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_platillos_restaurante_nombre_corto",
                table: "platillos",
                columns: new[] { "restaurante_id", "nombre_corto_normalizado" },
                unique: true,
                filter: "eliminada_en IS NULL AND nombre_corto IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "platillos");
        }
    }
}
