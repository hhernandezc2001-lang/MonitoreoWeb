using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MonitoreoWeb.Migrations
{
    /// <inheritdoc />
    public partial class AgregarTelegramChatIdACliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Cliente",
                columns: table => new
                {
                    IdCliente = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Apellido = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    TelegramChatId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cliente", x => x.IdCliente);
                });

            migrationBuilder.CreateTable(
                name: "Usuario",
                columns: table => new
                {
                    IdUsuario = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DebeCambiarPassword = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuario", x => x.IdUsuario);
                });

            migrationBuilder.CreateTable(
                name: "Dispositivo",
                columns: table => new
                {
                    IdDispositivo = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdCliente = table.Column<int>(type: "int", nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Modelo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IMEI = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FechaRegistro = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    ClienteIdCliente = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dispositivo", x => x.IdDispositivo);
                    table.ForeignKey(
                        name: "FK_Dispositivo_Cliente_ClienteIdCliente",
                        column: x => x.ClienteIdCliente,
                        principalTable: "Cliente",
                        principalColumn: "IdCliente");
                    table.ForeignKey(
                        name: "FK_Dispositivo_Cliente_IdCliente",
                        column: x => x.IdCliente,
                        principalTable: "Cliente",
                        principalColumn: "IdCliente",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reparacion",
                columns: table => new
                {
                    IdReparacion = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdDispositivo = table.Column<int>(type: "int", nullable: false),
                    IdTecnico = table.Column<int>(type: "int", nullable: true),
                    IdEstado = table.Column<int>(type: "int", nullable: false),
                    FechaIngreso = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaEntregaEstimada = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaTerminacion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FechaEntregaCliente = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProblemaReportado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Diagnostico = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CostoEstimado = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    CostoFinal = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    DiasGarantia = table.Column<int>(type: "int", nullable: false),
                    EsGarantia = table.Column<bool>(type: "bit", nullable: false),
                    IdReparacionOriginal = table.Column<int>(type: "int", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    Prioridad = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirmaAdminEntregaBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IdUsuario = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reparacion", x => x.IdReparacion);
                    table.ForeignKey(
                        name: "FK_Reparacion_Dispositivo_IdDispositivo",
                        column: x => x.IdDispositivo,
                        principalTable: "Dispositivo",
                        principalColumn: "IdDispositivo",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reparacion_Usuario_IdTecnico",
                        column: x => x.IdTecnico,
                        principalTable: "Usuario",
                        principalColumn: "IdUsuario");
                    table.ForeignKey(
                        name: "FK_Reparacion_Usuario_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuario",
                        principalColumn: "IdUsuario");
                });

            migrationBuilder.CreateTable(
                name: "ChecklistRecepcion",
                columns: table => new
                {
                    IdChecklist = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdReparacion = table.Column<int>(type: "int", nullable: false),
                    Enciende = table.Column<bool>(type: "bit", nullable: false),
                    PantallaEstrellada = table.Column<bool>(type: "bit", nullable: false),
                    TapaTraseraDañada = table.Column<bool>(type: "bit", nullable: false),
                    RayonesVisibles = table.Column<bool>(type: "bit", nullable: false),
                    HumedadOAgua = table.Column<bool>(type: "bit", nullable: false),
                    CargaCorrectamente = table.Column<bool>(type: "bit", nullable: false),
                    BotonesFuncionan = table.Column<bool>(type: "bit", nullable: false),
                    CamaraFrontalOk = table.Column<bool>(type: "bit", nullable: false),
                    CamaraTraseraOk = table.Column<bool>(type: "bit", nullable: false),
                    AltavozMicOk = table.Column<bool>(type: "bit", nullable: false),
                    LeeSIM = table.Column<bool>(type: "bit", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FirmaClienteBase64 = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChecklistRecepcion", x => x.IdChecklist);
                    table.ForeignKey(
                        name: "FK_ChecklistRecepcion_Reparacion_IdReparacion",
                        column: x => x.IdReparacion,
                        principalTable: "Reparacion",
                        principalColumn: "IdReparacion",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistorialAvance",
                columns: table => new
                {
                    IdAvance = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdReparacion = table.Column<int>(type: "int", nullable: false),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialAvance", x => x.IdAvance);
                    table.ForeignKey(
                        name: "FK_HistorialAvance_Reparacion_IdReparacion",
                        column: x => x.IdReparacion,
                        principalTable: "Reparacion",
                        principalColumn: "IdReparacion",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HistorialAvance_Usuario_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuario",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pago",
                columns: table => new
                {
                    IdPago = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdReparacion = table.Column<int>(type: "int", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaPago = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MetodoPago = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pago", x => x.IdPago);
                    table.ForeignKey(
                        name: "FK_Pago_Reparacion_IdReparacion",
                        column: x => x.IdReparacion,
                        principalTable: "Reparacion",
                        principalColumn: "IdReparacion",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TokenConsulta",
                columns: table => new
                {
                    IdToken = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdReparacion = table.Column<int>(type: "int", nullable: false),
                    CodigoUnico = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaExpiracion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenConsulta", x => x.IdToken);
                    table.ForeignKey(
                        name: "FK_TokenConsulta_Reparacion_IdReparacion",
                        column: x => x.IdReparacion,
                        principalTable: "Reparacion",
                        principalColumn: "IdReparacion",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FotoAvance",
                columns: table => new
                {
                    IdFoto = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdReparacion = table.Column<int>(type: "int", nullable: false),
                    IdAvance = table.Column<int>(type: "int", nullable: false),
                    IdUsuario = table.Column<int>(type: "int", nullable: false),
                    RutaArchivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FechaSubida = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VisibleCliente = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FotoAvance", x => x.IdFoto);
                    table.ForeignKey(
                        name: "FK_FotoAvance_HistorialAvance_IdAvance",
                        column: x => x.IdAvance,
                        principalTable: "HistorialAvance",
                        principalColumn: "IdAvance",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FotoAvance_Reparacion_IdReparacion",
                        column: x => x.IdReparacion,
                        principalTable: "Reparacion",
                        principalColumn: "IdReparacion",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FotoAvance_Usuario_IdUsuario",
                        column: x => x.IdUsuario,
                        principalTable: "Usuario",
                        principalColumn: "IdUsuario",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChecklistRecepcion_IdReparacion",
                table: "ChecklistRecepcion",
                column: "IdReparacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Dispositivo_ClienteIdCliente",
                table: "Dispositivo",
                column: "ClienteIdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_Dispositivo_IdCliente",
                table: "Dispositivo",
                column: "IdCliente");

            migrationBuilder.CreateIndex(
                name: "IX_FotoAvance_IdAvance",
                table: "FotoAvance",
                column: "IdAvance");

            migrationBuilder.CreateIndex(
                name: "IX_FotoAvance_IdReparacion",
                table: "FotoAvance",
                column: "IdReparacion");

            migrationBuilder.CreateIndex(
                name: "IX_FotoAvance_IdUsuario",
                table: "FotoAvance",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialAvance_IdReparacion",
                table: "HistorialAvance",
                column: "IdReparacion");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialAvance_IdUsuario",
                table: "HistorialAvance",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_Pago_IdReparacion",
                table: "Pago",
                column: "IdReparacion");

            migrationBuilder.CreateIndex(
                name: "IX_Reparacion_IdDispositivo",
                table: "Reparacion",
                column: "IdDispositivo");

            migrationBuilder.CreateIndex(
                name: "IX_Reparacion_IdTecnico",
                table: "Reparacion",
                column: "IdTecnico");

            migrationBuilder.CreateIndex(
                name: "IX_Reparacion_IdUsuario",
                table: "Reparacion",
                column: "IdUsuario");

            migrationBuilder.CreateIndex(
                name: "IX_TokenConsulta_IdReparacion",
                table: "TokenConsulta",
                column: "IdReparacion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChecklistRecepcion");

            migrationBuilder.DropTable(
                name: "FotoAvance");

            migrationBuilder.DropTable(
                name: "Pago");

            migrationBuilder.DropTable(
                name: "TokenConsulta");

            migrationBuilder.DropTable(
                name: "HistorialAvance");

            migrationBuilder.DropTable(
                name: "Reparacion");

            migrationBuilder.DropTable(
                name: "Dispositivo");

            migrationBuilder.DropTable(
                name: "Usuario");

            migrationBuilder.DropTable(
                name: "Cliente");
        }
    }
}
