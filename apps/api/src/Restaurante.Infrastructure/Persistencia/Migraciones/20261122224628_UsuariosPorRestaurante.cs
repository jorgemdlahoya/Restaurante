using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Restaurante.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class UsuariosPorRestaurante : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_usuarios_nombre_usuario_normalizado",
                table: "usuarios");

            migrationBuilder.AddColumn<Guid>(
                name: "restaurante_id",
                table: "usuarios",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_restaurante_nombre",
                table: "usuarios",
                columns: new[] { "restaurante_id", "nombre_usuario_normalizado" },
                unique: true)
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.AddForeignKey(
                name: "FK_usuarios_restaurantes_restaurante_id",
                table: "usuarios",
                column: "restaurante_id",
                principalTable: "restaurantes",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_usuarios_restaurantes_restaurante_id",
                table: "usuarios");

            migrationBuilder.DropIndex(
                name: "ix_usuarios_restaurante_nombre",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "restaurante_id",
                table: "usuarios");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_nombre_usuario_normalizado",
                table: "usuarios",
                column: "nombre_usuario_normalizado",
                unique: true);
        }
    }
}
