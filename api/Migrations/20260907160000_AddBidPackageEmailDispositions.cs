using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Bid package email dispositions (2026-09-07): the package's verdict on each of its tagged
    /// emails on the tender-response leg — Discarded (not a tender) or Extracted (became a quote).
    /// The mailbox tag stays the only link; this table never changes it. No FKs (house style);
    /// additive only, so it is safe before or with the deploy. Id timestamped AFTER every
    /// migration already on disk (20260907120000) so the scoped "script from the last applied id"
    /// flow cannot skip it.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260907160000_AddBidPackageEmailDispositions")]
    public partial class AddBidPackageEmailDispositions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BidPackageEmailDispositions",
                columns: table => new
                {
                    BidPackageEmailDispositionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BidPackageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    InternetMessageId = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    Outcome = table.Column<int>(type: "int", nullable: false),
                    QuoteId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Note = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    SetByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SetAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_BidPackageEmailDispositions", x => x.BidPackageEmailDispositionId));

            migrationBuilder.CreateIndex(
                name: "IX_BidPackageEmailDispositions_BidPackageId",
                table: "BidPackageEmailDispositions",
                column: "BidPackageId");
            migrationBuilder.CreateIndex(
                name: "IX_BidPackageEmailDispositions_InternetMessageId",
                table: "BidPackageEmailDispositions",
                column: "InternetMessageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BidPackageEmailDispositions");
        }
    }
}
