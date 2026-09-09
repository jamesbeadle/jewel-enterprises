using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Holds the HMRC CIS verification result properly (2026-09-09, the accountant's ask): the
    /// verification number and the date get their own columns instead of being squeezed into
    /// CisStatus, which is widened from 32 to 64 so a full status reading fits. Additive only.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260909105000_AddCisVerification")]
    public partial class AddCisVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CisStatus", table: "Subcontractors", type: "nvarchar(64)", maxLength: 64, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(32)", oldMaxLength: 32);

            migrationBuilder.AddColumn<string>(
                name: "CisVerificationNumber", table: "Subcontractors", type: "nvarchar(32)", maxLength: 32, nullable: false, defaultValue: "");

            migrationBuilder.AddColumn<DateOnly>(
                name: "CisVerifiedOn", table: "Subcontractors", type: "date", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CisVerifiedOn", table: "Subcontractors");
            migrationBuilder.DropColumn(name: "CisVerificationNumber", table: "Subcontractors");

            migrationBuilder.AlterColumn<string>(
                name: "CisStatus", table: "Subcontractors", type: "nvarchar(32)", maxLength: 32, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(64)", oldMaxLength: 64);
        }
    }
}
