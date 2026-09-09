using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// A work-order link may name the cost centre its share sits on (2026-09-09, the accountant's
    /// ask): a Work Order bill split across two orders writes one link per order AND code, so the
    /// financial summary lands each order's money on the right centre. The unique (line, order)
    /// index becomes (line, order, code); hand links keep a null code, so one per (line, order)
    /// as before. Additive — apply before or with the deploy.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260909150000_AddXeroLineWorkOrderLinkCostCenterCode")]
    public partial class AddXeroLineWorkOrderLinkCostCenterCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CostCenterCode", table: "XeroLineWorkOrderLinks", type: "nvarchar(32)", maxLength: 32, nullable: true);

            migrationBuilder.DropIndex(name: "UX_XeroLineWorkOrderLinks_Line_Order", table: "XeroLineWorkOrderLinks");

            migrationBuilder.CreateIndex(
                name: "UX_XeroLineWorkOrderLinks_Line_Order_Code",
                table: "XeroLineWorkOrderLinks", columns: new[] { "XeroLedgerLineId", "WorkOrderId", "CostCenterCode" }, unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "UX_XeroLineWorkOrderLinks_Line_Order_Code", table: "XeroLineWorkOrderLinks");

            migrationBuilder.CreateIndex(
                name: "UX_XeroLineWorkOrderLinks_Line_Order",
                table: "XeroLineWorkOrderLinks", columns: new[] { "XeroLedgerLineId", "WorkOrderId" }, unique: true);

            migrationBuilder.DropColumn(name: "CostCenterCode", table: "XeroLineWorkOrderLinks");
        }
    }
}
