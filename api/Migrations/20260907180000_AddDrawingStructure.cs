using System;
using Jewel.JPMS.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <summary>
    /// The drawing extraction's own read of the PDF (2026-09-07): refs to the positioned-geometry
    /// blob and the structured-read blob, the title-block summary (drawing number, revision,
    /// scale, whether the sheet proved its scale) and the counts a register shows without opening
    /// a blob — plus MarkupsNote, why Bluebeam markups are absent when they are. All nullable,
    /// additive only: safe before or with the deploy. Id timestamped after every migration already
    /// on disk.
    /// </summary>
    [DbContext(typeof(JpmsContext))]
    [Migration("20260907180000_AddDrawingStructure")]
    public partial class AddDrawingStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MarkupsNote", table: "DrawingExtractions", type: "nvarchar(1024)", maxLength: 1024, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "GeometryBlobRef", table: "DrawingExtractions", type: "nvarchar(1024)", maxLength: 1024, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "StructureBlobRef", table: "DrawingExtractions", type: "nvarchar(1024)", maxLength: 1024, nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "DimensionCount", table: "DrawingExtractions", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "CalloutCount", table: "DrawingExtractions", type: "int", nullable: true);
            migrationBuilder.AddColumn<int>(
                name: "ShapeCount", table: "DrawingExtractions", type: "int", nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "Scale", table: "DrawingExtractions", type: "nvarchar(32)", maxLength: 32, nullable: true);
            migrationBuilder.AddColumn<bool>(
                name: "ScaleVerified", table: "DrawingExtractions", type: "bit", nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "DrawingNumber", table: "DrawingExtractions", type: "nvarchar(128)", maxLength: 128, nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "RevisionLabel", table: "DrawingExtractions", type: "nvarchar(32)", maxLength: 32, nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "RevisionLabel", table: "DrawingExtractions");
            migrationBuilder.DropColumn(name: "DrawingNumber", table: "DrawingExtractions");
            migrationBuilder.DropColumn(name: "ScaleVerified", table: "DrawingExtractions");
            migrationBuilder.DropColumn(name: "Scale", table: "DrawingExtractions");
            migrationBuilder.DropColumn(name: "ShapeCount", table: "DrawingExtractions");
            migrationBuilder.DropColumn(name: "CalloutCount", table: "DrawingExtractions");
            migrationBuilder.DropColumn(name: "DimensionCount", table: "DrawingExtractions");
            migrationBuilder.DropColumn(name: "StructureBlobRef", table: "DrawingExtractions");
            migrationBuilder.DropColumn(name: "GeometryBlobRef", table: "DrawingExtractions");
            migrationBuilder.DropColumn(name: "MarkupsNote", table: "DrawingExtractions");
        }
    }
}
