using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Work Order bill approvals (2026-09-08, the accountant's ask): the record of each Approve on
    /// the allocation page's Work Order bills tab — bill, order, matching rule, who and when, and
    /// who undid it if anyone. A brand-new table, nothing existing touched: safe before or with
    /// the deploy.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260908180000_AddWorkOrderBillApprovals")]
    public partial class AddWorkOrderBillApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WorkOrderBillApprovals",
                columns: table => new
                {
                    WorkOrderBillApprovalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    XeroInvoiceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkOrderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MatchRule = table.Column<int>(type: "int", nullable: false),
                    MatchDetail = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    BillNet = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ApprovedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ApprovedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UndoneByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UndoneAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrderBillApprovals", x => x.WorkOrderBillApprovalId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOrderBillApprovals_XeroInvoiceId",
                table: "WorkOrderBillApprovals",
                column: "XeroInvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "WorkOrderBillApprovals");
        }
    }
}
