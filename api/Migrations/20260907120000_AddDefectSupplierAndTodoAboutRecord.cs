using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// Defects raised WITH a supplier + to-dos ABOUT a record (2026-09-07).
    /// Defects: SubcontractorId (the directory record the defect is raised with — no FK, house
    /// style), SentToSupplierAt / SentToSupplierByEmail (first send from the defect's page; the
    /// compose pipeline stamps them). TodoItems: AboutRecordType / AboutRecordId — the record a
    /// to-do is about (a defect first; any record type later), indexed on the id for the record
    /// page's "to-dos about this" read. All nullable, additive only: safe before or with the
    /// deploy. Id timestamped AFTER every migration already on disk (the 2026-08-29 lesson).
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260907120000_AddDefectSupplierAndTodoAboutRecord")]
    public partial class AddDefectSupplierAndTodoAboutRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubcontractorId", table: "Defects", type: "nvarchar(64)", maxLength: 64, nullable: true);
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SentToSupplierAt", table: "Defects", type: "datetimeoffset", nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "SentToSupplierByEmail", table: "Defects", type: "nvarchar(256)", maxLength: 256, nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AboutRecordType", table: "TodoItems", type: "int", nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "AboutRecordId", table: "TodoItems", type: "nvarchar(64)", maxLength: 64, nullable: true);
            migrationBuilder.CreateIndex(name: "IX_TodoItems_AboutRecordId", table: "TodoItems", column: "AboutRecordId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(name: "IX_TodoItems_AboutRecordId", table: "TodoItems");
            migrationBuilder.DropColumn(name: "AboutRecordId", table: "TodoItems");
            migrationBuilder.DropColumn(name: "AboutRecordType", table: "TodoItems");
            migrationBuilder.DropColumn(name: "SentToSupplierByEmail", table: "Defects");
            migrationBuilder.DropColumn(name: "SentToSupplierAt", table: "Defects");
            migrationBuilder.DropColumn(name: "SubcontractorId", table: "Defects");
        }
    }
}
