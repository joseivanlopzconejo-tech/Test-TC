using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TorreControl.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class RolesBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Rol",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { 1, "rol-1", "Monitorista", "MONITORISTA" },
                    { 2, "rol-2", "SupervisorApoyo", "SUPERVISORAPOYO" },
                    { 3, "rol-3", "SupervisorOperaciones", "SUPERVISOROPERACIONES" },
                    { 4, "rol-4", "JefeMonitoreo", "JEFEMONITOREO" },
                    { 5, "rol-5", "Sistemas", "SISTEMAS" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Rol",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Rol",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Rol",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Rol",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Rol",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
