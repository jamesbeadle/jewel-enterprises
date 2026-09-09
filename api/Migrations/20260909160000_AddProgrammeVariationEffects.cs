using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Variations and Extensions of Time on the programme (2026-09-09). ProgrammeVariationEffects is
    /// the programme's own record of the days a variation pushes one of its tasks — kept beside
    /// the programme, never on the variation, one row per variation per task. Requests gains the
    /// EOT's days claimed / granted so the Programme tab can draw the extension from completion.
    /// Additive only, no FKs (the string-keyed arrangement of every programme table). Id
    /// timestamped AFTER every migration already on disk.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260909160000_AddProgrammeVariationEffects")]
    public partial class AddProgrammeVariationEffects : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProgrammeVariationEffects",
                columns: table => new
                {
                    ProgrammeVariationEffectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    VariationOrderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProgrammeTaskId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DelayDays = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    RecordedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ProgrammeVariationEffects", x => x.ProgrammeVariationEffectId));

            migrationBuilder.CreateIndex(name: "IX_ProgrammeVariationEffects_ProjectId", table: "ProgrammeVariationEffects", column: "ProjectId");
            migrationBuilder.CreateIndex(name: "IX_ProgrammeVariationEffects_VariationOrderId_ProgrammeTaskId", table: "ProgrammeVariationEffects", columns: new[] { "VariationOrderId", "ProgrammeTaskId" }, unique: true);

            migrationBuilder.AddColumn<int>(
                name: "EotDaysClaimed", table: "Requests", type: "int", nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EotDaysGranted", table: "Requests", type: "int", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "EotDaysGranted", table: "Requests");
            migrationBuilder.DropColumn(name: "EotDaysClaimed", table: "Requests");
            migrationBuilder.DropTable(name: "ProgrammeVariationEffects");
        }
    }
}
