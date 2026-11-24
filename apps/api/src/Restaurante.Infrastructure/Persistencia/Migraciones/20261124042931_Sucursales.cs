using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurante.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Sucursales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sucursales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    direccion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    telefono = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    slug = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    whatsapp = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    phone_number_id = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    prefijo = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    zona_horaria = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false),
                    creada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sucursales", x => x.id);
                    table.ForeignKey(
                        name: "FK_sucursales_restaurantes_restaurante_id",
                        column: x => x.restaurante_id,
                        principalTable: "restaurantes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cierres_temporales",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    restaurante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sucursal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    fin = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    motivo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cancelado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cierres_temporales", x => x.id);
                    table.ForeignKey(
                        name: "FK_cierres_temporales_sucursales_sucursal_id",
                        column: x => x.sucursal_id,
                        principalTable: "sucursales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "horarios_sucursal",
                columns: table => new
                {
                    sucursal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    dia_semana = table.Column<short>(type: "smallint", nullable: false),
                    restaurante_id = table.Column<Guid>(type: "uuid", nullable: false),
                    apertura = table.Column<TimeOnly>(type: "time", nullable: true),
                    cierre = table.Column<TimeOnly>(type: "time", nullable: true),
                    cerrado = table.Column<bool>(type: "boolean", nullable: false, computedColumnSql: "apertura IS NULL", stored: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_horarios_sucursal", x => new { x.sucursal_id, x.dia_semana });
                    table.CheckConstraint("ck_horarios_sucursal_dia_semana", "dia_semana BETWEEN 0 AND 6");
                    table.ForeignKey(
                        name: "FK_horarios_sucursal_sucursales_sucursal_id",
                        column: x => x.sucursal_id,
                        principalTable: "sucursales",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_cierres_temporales_sucursal_fin",
                table: "cierres_temporales",
                columns: new[] { "sucursal_id", "fin" });

            migrationBuilder.CreateIndex(
                name: "ix_sucursales_phone_number_id",
                table: "sucursales",
                column: "phone_number_id",
                unique: true,
                filter: "phone_number_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_sucursales_restaurante_nombre",
                table: "sucursales",
                columns: new[] { "restaurante_id", "nombre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sucursales_restaurante_prefijo",
                table: "sucursales",
                columns: new[] { "restaurante_id", "prefijo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sucursales_restaurante_slug",
                table: "sucursales",
                columns: new[] { "restaurante_id", "slug" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sucursales_whatsapp",
                table: "sucursales",
                column: "whatsapp",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cierres_temporales");

            migrationBuilder.DropTable(
                name: "horarios_sucursal");

            migrationBuilder.DropTable(
                name: "sucursales");
        }
    }
}
