using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Drops CompanyContacts.Purpose (2026-09-09): the field was read only as the caption on an
    /// email-picker chip, was almost always blank, and read as a question about Jewel's side rather
    /// than the contact's. Step two of two, DESTRUCTIVE: apply only after the code that stops
    /// reading the column is deployed (step one, DefaultCompanyContactPurpose, goes before it).
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260909110000_DropCompanyContactPurpose")]
    public partial class DropCompanyContactPurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Purpose", table: "CompanyContacts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Purpose", table: "CompanyContacts", type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValue: "");
        }
    }
}
