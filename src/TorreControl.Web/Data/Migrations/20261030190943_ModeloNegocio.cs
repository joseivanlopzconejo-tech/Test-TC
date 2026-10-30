using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TorreControl.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ModeloNegocio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Auditoria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: true),
                    Transaccion = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Entidad = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EntidadId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Campo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ValorAnterior = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValorNuevo = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Auditoria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cliente",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cliente", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EstatusUnidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Color = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EstatusUnidad", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Modulo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modulo", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Operador",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Clave = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Supervisor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Coordinador = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Operador", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TipoIncidencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TipoIncidencia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Unidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unidad", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Planta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClienteId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Municipio = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Planta_Cliente_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Cliente",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModuloUsuario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuloId = table.Column<int>(type: "int", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuloUsuario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuloUsuario_Modulo_ModuloId",
                        column: x => x.ModuloId,
                        principalTable: "Modulo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModuloUsuario_Usuario_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ModuloPlanta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ModuloId = table.Column<int>(type: "int", nullable: false),
                    PlantaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuloPlanta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuloPlanta_Modulo_ModuloId",
                        column: x => x.ModuloId,
                        principalTable: "Modulo",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ModuloPlanta_Planta_PlantaId",
                        column: x => x.PlantaId,
                        principalTable: "Planta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Ruta",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlantaId = table.Column<int>(type: "int", nullable: false),
                    NombreRuta = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TipoServicio = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ruta", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ruta_Planta_PlantaId",
                        column: x => x.PlantaId,
                        principalTable: "Planta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Servicio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RutaId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Turno = table.Column<int>(type: "int", nullable: false),
                    Hip = table.Column<TimeOnly>(type: "time", nullable: false),
                    Htp = table.Column<TimeOnly>(type: "time", nullable: false),
                    Hir = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Htr = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HirModificaciones = table.Column<int>(type: "int", nullable: false),
                    HtrModificaciones = table.Column<int>(type: "int", nullable: false),
                    EstatusId = table.Column<int>(type: "int", nullable: true),
                    Aforo = table.Column<int>(type: "int", nullable: true),
                    UnidadTitularId = table.Column<int>(type: "int", nullable: true),
                    OperadorId = table.Column<int>(type: "int", nullable: true),
                    EsEspecial = table.Column<bool>(type: "bit", nullable: false),
                    CreadoPorId = table.Column<int>(type: "int", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servicio", x => x.Id);
                    table.CheckConstraint("CK_Servicio_Aforo", "[Aforo] IS NULL OR [Aforo] >= 0");
                    table.CheckConstraint("CK_Servicio_Turno", "[Turno] BETWEEN 1 AND 3");
                    table.ForeignKey(
                        name: "FK_Servicio_EstatusUnidad_EstatusId",
                        column: x => x.EstatusId,
                        principalTable: "EstatusUnidad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Servicio_Operador_OperadorId",
                        column: x => x.OperadorId,
                        principalTable: "Operador",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Servicio_Ruta_RutaId",
                        column: x => x.RutaId,
                        principalTable: "Ruta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Servicio_Unidad_UnidadTitularId",
                        column: x => x.UnidadTitularId,
                        principalTable: "Unidad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Servicio_Usuario_CreadoPorId",
                        column: x => x.CreadoPorId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Filtro",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServicioId = table.Column<int>(type: "int", nullable: false),
                    NumeroFiltro = table.Column<int>(type: "int", nullable: false),
                    HoraProgramada = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EstatusId = table.Column<int>(type: "int", nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CapturadoEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CapturadoPorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Filtro", x => x.Id);
                    table.CheckConstraint("CK_Filtro_Numero", "[NumeroFiltro] BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_Filtro_EstatusUnidad_EstatusId",
                        column: x => x.EstatusId,
                        principalTable: "EstatusUnidad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Filtro_Servicio_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Filtro_Usuario_CapturadoPorId",
                        column: x => x.CapturadoPorId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Incidencia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServicioId = table.Column<int>(type: "int", nullable: false),
                    Momento = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    TipoIncidenciaId = table.Column<int>(type: "int", nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Texto = table.Column<string>(type: "nvarchar(700)", maxLength: 700, nullable: false),
                    EsSinNovedad = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidencia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Incidencia_Servicio_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Incidencia_TipoIncidencia_TipoIncidenciaId",
                        column: x => x.TipoIncidenciaId,
                        principalTable: "TipoIncidencia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UnidadApoyo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServicioId = table.Column<int>(type: "int", nullable: false),
                    UnidadId = table.Column<int>(type: "int", nullable: false),
                    Orden = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadApoyo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UnidadApoyo_Servicio_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicio",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnidadApoyo_Unidad_UnidadId",
                        column: x => x.UnidadId,
                        principalTable: "Unidad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Modificacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FiltroId = table.Column<int>(type: "int", nullable: false),
                    NumeroModificacion = table.Column<int>(type: "int", nullable: false),
                    EstatusId = table.Column<int>(type: "int", nullable: false),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CapturadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CapturadoPorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Modificacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Modificacion_EstatusUnidad_EstatusId",
                        column: x => x.EstatusId,
                        principalTable: "EstatusUnidad",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Modificacion_Filtro_FiltroId",
                        column: x => x.FiltroId,
                        principalTable: "Filtro",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Modificacion_Usuario_CapturadoPorId",
                        column: x => x.CapturadoPorId,
                        principalTable: "Usuario",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "EstatusUnidad",
                columns: new[] { "Id", "Color", "Nombre" },
                values: new object[,]
                {
                    { 1, "#D64545", "Apagada" },
                    { 2, "#E0B020", "Trayecto" },
                    { 3, "#2E9E5B", "Finalizado" }
                });

            migrationBuilder.InsertData(
                table: "TipoIncidencia",
                columns: new[] { "Id", "Nombre" },
                values: new object[,]
                {
                    { 1, "Tráfico" },
                    { 2, "Falla mecánica" },
                    { 3, "Operador" },
                    { 4, "Cliente" },
                    { 5, "Otro" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_Entidad_EntidadId",
                table: "Auditoria",
                columns: new[] { "Entidad", "EntidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_FechaUtc",
                table: "Auditoria",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Auditoria_UsuarioId",
                table: "Auditoria",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Cliente_Codigo",
                table: "Cliente",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EstatusUnidad_Nombre",
                table: "EstatusUnidad",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Filtro_CapturadoPorId",
                table: "Filtro",
                column: "CapturadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Filtro_EstatusId",
                table: "Filtro",
                column: "EstatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Filtro_ServicioId_NumeroFiltro",
                table: "Filtro",
                columns: new[] { "ServicioId", "NumeroFiltro" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Incidencia_ServicioId_Momento",
                table: "Incidencia",
                columns: new[] { "ServicioId", "Momento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Incidencia_TipoIncidenciaId",
                table: "Incidencia",
                column: "TipoIncidenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_Modificacion_CapturadoPorId",
                table: "Modificacion",
                column: "CapturadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Modificacion_EstatusId",
                table: "Modificacion",
                column: "EstatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Modificacion_FiltroId_NumeroModificacion",
                table: "Modificacion",
                columns: new[] { "FiltroId", "NumeroModificacion" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Modulo_Nombre",
                table: "Modulo",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuloPlanta_ModuloId_PlantaId",
                table: "ModuloPlanta",
                columns: new[] { "ModuloId", "PlantaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuloPlanta_PlantaId",
                table: "ModuloPlanta",
                column: "PlantaId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuloUsuario_ModuloId",
                table: "ModuloUsuario",
                column: "ModuloId");

            migrationBuilder.CreateIndex(
                name: "IX_ModuloUsuario_UsuarioId_FechaInicio",
                table: "ModuloUsuario",
                columns: new[] { "UsuarioId", "FechaInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Operador_Clave",
                table: "Operador",
                column: "Clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Planta_ClienteId",
                table: "Planta",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Ruta_PlantaId_NombreRuta_TipoServicio",
                table: "Ruta",
                columns: new[] { "PlantaId", "NombreRuta", "TipoServicio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Servicio_CreadoPorId",
                table: "Servicio",
                column: "CreadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicio_EstatusId",
                table: "Servicio",
                column: "EstatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicio_Fecha",
                table: "Servicio",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Servicio_OperadorId",
                table: "Servicio",
                column: "OperadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicio_RutaId_Fecha",
                table: "Servicio",
                columns: new[] { "RutaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Servicio_UnidadTitularId",
                table: "Servicio",
                column: "UnidadTitularId");

            migrationBuilder.CreateIndex(
                name: "IX_TipoIncidencia_Nombre",
                table: "TipoIncidencia",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Unidad_Numero",
                table: "Unidad",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnidadApoyo_ServicioId_Orden",
                table: "UnidadApoyo",
                columns: new[] { "ServicioId", "Orden" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnidadApoyo_ServicioId_UnidadId",
                table: "UnidadApoyo",
                columns: new[] { "ServicioId", "UnidadId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UnidadApoyo_UnidadId",
                table: "UnidadApoyo",
                column: "UnidadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Auditoria");

            migrationBuilder.DropTable(
                name: "Incidencia");

            migrationBuilder.DropTable(
                name: "Modificacion");

            migrationBuilder.DropTable(
                name: "ModuloPlanta");

            migrationBuilder.DropTable(
                name: "ModuloUsuario");

            migrationBuilder.DropTable(
                name: "UnidadApoyo");

            migrationBuilder.DropTable(
                name: "TipoIncidencia");

            migrationBuilder.DropTable(
                name: "Filtro");

            migrationBuilder.DropTable(
                name: "Modulo");

            migrationBuilder.DropTable(
                name: "Servicio");

            migrationBuilder.DropTable(
                name: "EstatusUnidad");

            migrationBuilder.DropTable(
                name: "Operador");

            migrationBuilder.DropTable(
                name: "Ruta");

            migrationBuilder.DropTable(
                name: "Unidad");

            migrationBuilder.DropTable(
                name: "Planta");

            migrationBuilder.DropTable(
                name: "Cliente");
        }
    }
}
