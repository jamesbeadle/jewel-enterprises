using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The public liability limit of indemnity a compliance document certifies (2026-09-10, the
    /// accountant's ask: Jewel's insurer requires £5m of every subcontractor on a big job, and the
    /// register only said an insurance document existed). One nullable money column on the
    /// document — null is "not recorded", never nil cover. Additive — apply before or with the
    /// deploy. Scoped script: api/Migrations/add-compliance-document-public-liability-cover.sql.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260911000000_AddComplianceDocumentPublicLiabilityCover")]
    public partial class AddComplianceDocumentPublicLiabilityCover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PublicLiabilityCover",
                table: "ComplianceDocuments",
                type: "decimal(18,4)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PublicLiabilityCover", table: "ComplianceDocuments");
        }
    }
}
