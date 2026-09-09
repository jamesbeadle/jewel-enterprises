using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The sales invoice raised in Xero from the portal (2026-09-09, the accountant's ask): the
    /// AUTHORISED ACCREC invoice's Xero id, number and when, stamped on the valuation invoice.
    /// Additive — apply before or with the deploy.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260910110000_AddValuationInvoiceXeroRaise")]
    public partial class AddValuationInvoiceXeroRaise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "XeroInvoiceId",
                table: "ValuationInvoices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "XeroInvoiceNumber",
                table: "ValuationInvoices",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "XeroRaisedAt",
                table: "ValuationInvoices",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "XeroInvoiceId", table: "ValuationInvoices");
            migrationBuilder.DropColumn(name: "XeroInvoiceNumber", table: "ValuationInvoices");
            migrationBuilder.DropColumn(name: "XeroRaisedAt", table: "ValuationInvoices");
        }
    }
}
