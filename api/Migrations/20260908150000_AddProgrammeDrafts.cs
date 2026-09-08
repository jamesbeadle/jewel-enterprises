using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Draft programme updates from the certified valuation (2026-09-08): a programme task's
    /// confirmed cost-centre mappings (ProgrammeTaskCostCentres), and the drafts opened when a
    /// valuation invoice's approval is recorded — one row per draft, one line per programme task
    /// with the proposal and the reviewer's decision. Additive only, no FKs (the same string-keyed
    /// arrangement as the programme tables); the handlers own every cascade. Id timestamped AFTER
    /// every migration already on disk.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260908150000_AddProgrammeDrafts")]
    public partial class AddProgrammeDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProgrammeTaskCostCentres",
                columns: table => new
                {
                    ProgrammeTaskCostCentreId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProgrammeTaskId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CostCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ProgrammeTaskCostCentres", x => x.ProgrammeTaskCostCentreId));

            migrationBuilder.CreateIndex(name: "IX_ProgrammeTaskCostCentres_ProjectId", table: "ProgrammeTaskCostCentres", column: "ProjectId");
            migrationBuilder.CreateIndex(name: "IX_ProgrammeTaskCostCentres_ProgrammeTaskId", table: "ProgrammeTaskCostCentres", column: "ProgrammeTaskId");

            migrationBuilder.CreateTable(
                name: "ProgrammeDrafts",
                columns: table => new
                {
                    ProgrammeDraftId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ValuationClaimId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ClaimName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResolvedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SuggestionsRequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SuggestionsNote = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_ProgrammeDrafts", x => x.ProgrammeDraftId));

            migrationBuilder.CreateIndex(name: "IX_ProgrammeDrafts_ProjectId_Status", table: "ProgrammeDrafts", columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateTable(
                name: "ProgrammeDraftLines",
                columns: table => new
                {
                    ProgrammeDraftLineId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProgrammeDraftId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProgrammeTaskId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    TaskTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CurrentPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ProposedPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CostCodes = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    MappingSource = table.Column<int>(type: "int", nullable: false),
                    Evidence = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    IsIncluded = table.Column<bool>(type: "bit", nullable: false),
                    ReviewedPercent = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_ProgrammeDraftLines", x => x.ProgrammeDraftLineId));

            migrationBuilder.CreateIndex(name: "IX_ProgrammeDraftLines_ProgrammeDraftId", table: "ProgrammeDraftLines", column: "ProgrammeDraftId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ProgrammeDraftLines");
            migrationBuilder.DropTable(name: "ProgrammeDrafts");
            migrationBuilder.DropTable(name: "ProgrammeTaskCostCentres");
        }
    }
}
