using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// When a ledger line's Xero write-back last failed (2026-09-08, the accountant's ask): kept
    /// alongside the last error text through a later success, so a bill that failed and then
    /// approved on retry still says so. Nullable, additive only — safe before or with the deploy.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260908200000_AddXeroLineWriteBackFailedAt")]
    public partial class AddXeroLineWriteBackFailedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "WriteBackFailedAtUtc", table: "XeroLedgerLines", type: "datetimeoffset", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "WriteBackFailedAtUtc", table: "XeroLedgerLines");
        }
    }
}
