using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The worker a timesheet cover settles (2026-09-08, the accountant's item J): the coding run
    /// recodes a company bill that covers several workers to every worker's lines at once and
    /// marks the cover per worker, so each worker's settlement schedule reconciles against their
    /// own lines. Nullable, additive only — a null cover is the counterparty's as a whole, which
    /// is what every cover marked before this column means. Safe before or with the deploy.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260908120000_AddWorkerToXeroLineTimesheetCovers")]
    public partial class AddWorkerToXeroLineTimesheetCovers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkerId", table: "XeroLineTimesheetCovers", type: "nvarchar(64)", maxLength: 64, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "WorkerId", table: "XeroLineTimesheetCovers");
        }
    }
}
