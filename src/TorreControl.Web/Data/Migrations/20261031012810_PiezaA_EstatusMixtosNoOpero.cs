using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TorreControl.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class PiezaA_EstatusMixtosNoOpero : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Servicio_EstatusUnidad_EstatusId",
                table: "Servicio");

            migrationBuilder.DropIndex(
                name: "IX_Servicio_EstatusId",
                table: "Servicio");

            migrationBuilder.DropColumn(
                name: "EstatusId",
                table: "Servicio");

            migrationBuilder.AddColumn<bool>(
                name: "EsMixto",
                table: "Servicio",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "NoOpero",
                table: "Servicio",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "NombreCorto",
                table: "Planta",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.UpdateData(
                table: "EstatusUnidad",
                keyColumn: "Id",
                keyValue: 1,
                column: "Color",
                value: "#F4A6A6");

            migrationBuilder.UpdateData(
                table: "EstatusUnidad",
                keyColumn: "Id",
                keyValue: 2,
                column: "Color",
                value: "#F6D88A");

            migrationBuilder.UpdateData(
                table: "EstatusUnidad",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Color", "Nombre" },
                values: new object[] { "#B5E8B0", "Encendida" });

            migrationBuilder.InsertData(
                table: "EstatusUnidad",
                columns: new[] { "Id", "Color", "Nombre" },
                values: new object[,]
                {
                    { 4, "#D9C3A5", "En base" },
                    { 5, "#CDB8F2", "Detenida" },
                    { 6, "#C8D0DA", "Pérdida de cobertura" },
                    { 7, "#A7B1BE", "Falla GPS" },
                    { 8, "#F2A27E", "Sin respuesta del operador" },
                    { 9, "#E89393", "Falla mecánica" }
                });

            migrationBuilder.UpdateData(
                table: "TipoIncidencia",
                keyColumn: "Id",
                keyValue: 4,
                column: "Nombre",
                value: "Cliente / planta");

            migrationBuilder.InsertData(
                table: "TipoIncidencia",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { 6, "Cruce de tren" },
                    { 7, "Accidente vial" },
                    { 8, "Bloqueo o manifestación" },
                    { 9, "Clima" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "EstatusUnidad",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "EstatusUnidad",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "EstatusUnidad",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "EstatusUnidad",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "EstatusUnidad",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "EstatusUnidad",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "TipoIncidencia",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "TipoIncidencia",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "TipoIncidencia",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "TipoIncidencia",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DropColumn(
                name: "EsMixto",
                table: "Servicio");

            migrationBuilder.DropColumn(
                name: "NoOpero",
                table: "Servicio");

            migrationBuilder.DropColumn(
                name: "NombreCorto",
                table: "Planta");

            migrationBuilder.AddColumn<int>(
                name: "EstatusId",
                table: "Servicio",
                type: "int",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "EstatusUnidad",
                keyColumn: "Id",
                keyValue: 1,
                column: "Color",
                value: "#D64545");

            migrationBuilder.UpdateData(
                table: "EstatusUnidad",
                keyColumn: "Id",
                keyValue: 2,
                column: "Color",
                value: "#E0B020");

            migrationBuilder.UpdateData(
                table: "EstatusUnidad",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Color", "Nombre" },
                values: new object[] { "#2E9E5B", "Finalizado" });

            migrationBuilder.UpdateData(
                table: "TipoIncidencia",
                keyColumn: "Id",
                keyValue: 4,
                column: "Nombre",
                value: "Cliente");

            migrationBuilder.CreateIndex(
                name: "IX_Servicio_EstatusId",
                table: "Servicio",
                column: "EstatusId");

            migrationBuilder.AddForeignKey(
                name: "FK_Servicio_EstatusUnidad_EstatusId",
                table: "Servicio",
                column: "EstatusId",
                principalTable: "EstatusUnidad",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
