using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Step one of two in dropping CompanyContacts.Purpose (2026-09-09). The column was NOT NULL
    /// with no default, so code that no longer writes it could not insert a contact; this gives it
    /// a default of "" so the new code deploys against the old schema. Additive — apply BEFORE the
    /// deploy. DropCompanyContactPurpose (step two) removes the column after it.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260909100000_DefaultCompanyContactPurpose")]
    public partial class DefaultCompanyContactPurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Purpose", table: "CompanyContacts", type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValue: "",
                oldClrType: typeof(string), oldType: "nvarchar(128)", oldMaxLength: 128);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Purpose", table: "CompanyContacts", type: "nvarchar(128)", maxLength: 128, nullable: false,
                oldClrType: typeof(string), oldType: "nvarchar(128)", oldMaxLength: 128, oldDefaultValue: "");
        }
    }
}
