using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The Xero contact a project's sales invoices are raised on (2026-09-10, the accountant's
    /// ask: the raise matched the client by name and created a duplicate contact on a miss).
    /// Xero's ContactID and the name as Xero holds it, mapped in Project settings beside the
    /// Sites option; Raise in Xero is blocked until it is set. Additive — apply before or with
    /// the deploy.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260910230000_AddProjectXeroContact")]
    public partial class AddProjectXeroContact : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "XeroContactId",
                table: "Projects",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "XeroContactName",
                table: "Projects",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "XeroContactId", table: "Projects");
            migrationBuilder.DropColumn(name: "XeroContactName", table: "Projects");
        }
    }
}
