using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jewel.JPMS.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccessRequests",
                columns: table => new
                {
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Provider = table.Column<int>(type: "int", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccessRequests", x => x.Email);
                });

            migrationBuilder.CreateTable(
                name: "BidDecisions",
                columns: table => new
                {
                    LeadId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ShouldBid = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    DecidedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BidDecisions", x => x.LeadId);
                });

            migrationBuilder.CreateTable(
                name: "BidPackages",
                columns: table => new
                {
                    BidPackageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Trade = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    OwnerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BidPackages", x => x.BidPackageId);
                });

            migrationBuilder.CreateTable(
                name: "BoqLineItems",
                columns: table => new
                {
                    BoqLineItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RateValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Discipline = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoqLineItems", x => x.BoqLineItemId);
                });

            migrationBuilder.CreateTable(
                name: "BoqSignOffs",
                columns: table => new
                {
                    BoqSignOffId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SignedOffByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SignedOffAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenderTotalAtSignOff = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoqSignOffs", x => x.BoqSignOffId);
                });

            migrationBuilder.CreateTable(
                name: "CashflowSnapshots",
                columns: table => new
                {
                    CashflowSnapshotId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ExpectedIncome13Week = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CommittedSpend13Week = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetPosition13Week = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CashflowSnapshots", x => x.CashflowSnapshotId);
                });

            migrationBuilder.CreateTable(
                name: "ChangeRecords",
                columns: table => new
                {
                    ChangeRecordId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    RaisedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RaisedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RespondedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResponseText = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: true),
                    RespondedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ImpliesVariation = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeRecords", x => x.ChangeRecordId);
                });

            migrationBuilder.CreateTable(
                name: "ClaimPeriods",
                columns: table => new
                {
                    ClaimPeriodId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PeriodNumber = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimPeriods", x => x.ClaimPeriodId);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceDocuments",
                columns: table => new
                {
                    ComplianceDocumentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubcontractorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ComplianceDocuments", x => x.ComplianceDocumentId);
                });

            migrationBuilder.CreateTable(
                name: "ContraCharges",
                columns: table => new
                {
                    ContraChargeId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubcontractorReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RaisedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    RecoveredAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContraCharges", x => x.ContraChargeId);
                });

            migrationBuilder.CreateTable(
                name: "CostCodeBudgets",
                columns: table => new
                {
                    CostCodeBudgetId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CostCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SpentAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCodeBudgets", x => x.CostCodeBudgetId);
                });

            migrationBuilder.CreateTable(
                name: "CostCodes",
                columns: table => new
                {
                    CostCodeId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CostCodes", x => x.CostCodeId);
                });

            migrationBuilder.CreateTable(
                name: "CvrPackageRows",
                columns: table => new
                {
                    CvrPackageRowId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PackageName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    OrderCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OrderValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VariationCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VariationValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MovementSinceLastSnapshot = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvrPackageRows", x => x.CvrPackageRowId);
                });

            migrationBuilder.CreateTable(
                name: "CvrSnapshots",
                columns: table => new
                {
                    CvrSnapshotId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SnapshotAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenderValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ForecastFinalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ForecastFinalValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MarginPounds = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MarginPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    WeeksAheadOrBehind = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CvrSnapshots", x => x.CvrSnapshotId);
                });

            migrationBuilder.CreateTable(
                name: "Dayworks",
                columns: table => new
                {
                    DayworkId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WorkedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SubcontractorReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    InstructedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LabourCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PlantCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaterialsCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UpliftPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ChargeableAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Dayworks", x => x.DayworkId);
                });

            migrationBuilder.CreateTable(
                name: "Defects",
                columns: table => new
                {
                    DefectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AssignedToEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RaisedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Defects", x => x.DefectId);
                });

            migrationBuilder.CreateTable(
                name: "DirectoryUserRoles",
                columns: table => new
                {
                    DirectoryUserRoleId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DirectoryUserEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectoryUserRoles", x => x.DirectoryUserRoleId);
                });

            migrationBuilder.CreateTable(
                name: "DirectoryUsers",
                columns: table => new
                {
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectoryUsers", x => x.Email);
                });

            migrationBuilder.CreateTable(
                name: "DrawingIssueRecords",
                columns: table => new
                {
                    DrawingIssueRecordId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DrawingRevisionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IssuedByName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrawingIssueRecords", x => x.DrawingIssueRecordId);
                });

            migrationBuilder.CreateTable(
                name: "DrawingRevisions",
                columns: table => new
                {
                    DrawingRevisionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DrawingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    RevisionLabel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IssuedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    SupersededAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsAmbiguous = table.Column<bool>(type: "bit", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DrawingRevisions", x => x.DrawingRevisionId);
                });

            migrationBuilder.CreateTable(
                name: "Drawings",
                columns: table => new
                {
                    DrawingId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    DrawingCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CurrentRevision = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drawings", x => x.DrawingId);
                });

            migrationBuilder.CreateTable(
                name: "Eots",
                columns: table => new
                {
                    EotId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    DaysGranted = table.Column<int>(type: "int", nullable: false),
                    CommercialRecovery = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrantedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Eots", x => x.EotId);
                });

            migrationBuilder.CreateTable(
                name: "ForecastComponents",
                columns: table => new
                {
                    ForecastComponentId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PackageName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CostIncurred = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostCommitted = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QsAccrualAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrelimForecast = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostToComplete = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastComponents", x => x.ForecastComponentId);
                });

            migrationBuilder.CreateTable(
                name: "HandoverPackItems",
                columns: table => new
                {
                    HandoverPackItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    IsReady = table.Column<bool>(type: "bit", nullable: false),
                    EvidenceBlobRef = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HandoverPackItems", x => x.HandoverPackItemId);
                });

            migrationBuilder.CreateTable(
                name: "HsRecordAttendance",
                columns: table => new
                {
                    HsRecordAttendanceId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    HsRecordId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AttendeeName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SignatureBlobRef = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SignedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HsRecordAttendance", x => x.HsRecordAttendanceId);
                });

            migrationBuilder.CreateTable(
                name: "HsRecords",
                columns: table => new
                {
                    HsRecordId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AssignedToEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RaisedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DueAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HsRecords", x => x.HsRecordId);
                });

            migrationBuilder.CreateTable(
                name: "InfoChaseItems",
                columns: table => new
                {
                    InfoChaseItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LeadId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    IsReceived = table.Column<bool>(type: "bit", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InfoChaseItems", x => x.InfoChaseItemId);
                });

            migrationBuilder.CreateTable(
                name: "LeadOutcomes",
                columns: table => new
                {
                    LeadId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsWon = table.Column<bool>(type: "bit", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    DecidedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    DecidedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeadOutcomes", x => x.LeadId);
                });

            migrationBuilder.CreateTable(
                name: "Leads",
                columns: table => new
                {
                    LeadId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SiteAddress = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    EstimatedValue = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Source = table.Column<int>(type: "int", nullable: false),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    OwnerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leads", x => x.LeadId);
                });

            migrationBuilder.CreateTable(
                name: "MobilisationItems",
                columns: table => new
                {
                    MobilisationItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    OwnerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MobilisationItems", x => x.MobilisationItemId);
                });

            migrationBuilder.CreateTable(
                name: "Photos",
                columns: table => new
                {
                    PhotoId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AttachedKind = table.Column<int>(type: "int", nullable: false),
                    AttachedId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    BlobUri = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    Caption = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    TakenByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    TakenAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GpsLatitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    GpsLongitude = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Photos", x => x.PhotoId);
                });

            migrationBuilder.CreateTable(
                name: "PracticalCompletions",
                columns: table => new
                {
                    PracticalCompletionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AchievedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CertificateBlobRef = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IssuedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    IsClientSigned = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PracticalCompletions", x => x.PracticalCompletionId);
                });

            migrationBuilder.CreateTable(
                name: "PrelimForecastEntries",
                columns: table => new
                {
                    PrelimForecastEntryId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PrelimItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    WeekNumber = table.Column<int>(type: "int", nullable: false),
                    TenderedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ForecastAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrelimForecastEntries", x => x.PrelimForecastEntryId);
                });

            migrationBuilder.CreateTable(
                name: "PrelimItems",
                columns: table => new
                {
                    PrelimItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrelimItems", x => x.PrelimItemId);
                });

            migrationBuilder.CreateTable(
                name: "ProgrammeTasks",
                columns: table => new
                {
                    ProgrammeTaskId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PlannedStart = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PlannedEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ProgressPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BoqLineItemId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammeTasks", x => x.ProgrammeTaskId);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ClientName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Organisation = table.Column<int>(type: "int", nullable: false),
                    Stage = table.Column<int>(type: "int", nullable: false),
                    ProjectManagerEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ProjectId);
                });

            migrationBuilder.CreateTable(
                name: "Proposals",
                columns: table => new
                {
                    ProposalId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LeadId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    NegotiationRoundsJson = table.Column<string>(type: "nvarchar(max)", maxLength: 8192, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Proposals", x => x.ProposalId);
                });

            migrationBuilder.CreateTable(
                name: "QsAccruals",
                columns: table => new
                {
                    QsAccrualId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    AddAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OmitAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LiabilityAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SignedOffByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SignedOffAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QsAccruals", x => x.QsAccrualId);
                });

            migrationBuilder.CreateTable(
                name: "QualificationAssessments",
                columns: table => new
                {
                    LeadId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    AssessedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    AssessedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QualificationAssessments", x => x.LeadId);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    QuoteId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BidPackageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubcontractorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsDeclined = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.QuoteId);
                });

            migrationBuilder.CreateTable(
                name: "Rates",
                columns: table => new
                {
                    RateId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Trade = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Unit = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SupplierName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    LastPricedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rates", x => x.RateId);
                });

            migrationBuilder.CreateTable(
                name: "RetentionReleases",
                columns: table => new
                {
                    RetentionReleaseId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsPublishedDownstream = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetentionReleases", x => x.RetentionReleaseId);
                });

            migrationBuilder.CreateTable(
                name: "SettlementRecords",
                columns: table => new
                {
                    SettlementRecordId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FinalContractValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FinalCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FinalMargin = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AgreedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsClientSigned = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SettlementRecords", x => x.SettlementRecordId);
                });

            migrationBuilder.CreateTable(
                name: "SiteReports",
                columns: table => new
                {
                    SiteReportId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PeriodEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Narrative = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    AttendanceDays = table.Column<int>(type: "int", nullable: false),
                    OpenSnags = table.Column<int>(type: "int", nullable: false),
                    ProgressPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsIssued = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteReports", x => x.SiteReportId);
                });

            migrationBuilder.CreateTable(
                name: "SiteVisits",
                columns: table => new
                {
                    SiteVisitId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    LeadId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ScheduledAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AttendeeEmailsCsv = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    PhotoCount = table.Column<int>(type: "int", nullable: false),
                    IsComplete = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteVisits", x => x.SiteVisitId);
                });

            migrationBuilder.CreateTable(
                name: "SubcontractorRetentions",
                columns: table => new
                {
                    SubcontractorRetentionId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubcontractorReference = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CertifiedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RetentionPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FirstReleasedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FinalReleasedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcontractorRetentions", x => x.SubcontractorRetentionId);
                });

            migrationBuilder.CreateTable(
                name: "Subcontractors",
                columns: table => new
                {
                    SubcontractorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PrimaryTrade = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ContactName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CisStatus = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OnboardedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subcontractors", x => x.SubcontractorId);
                });

            migrationBuilder.CreateTable(
                name: "Timesheets",
                columns: table => new
                {
                    TimesheetId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PersonEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    WorkedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Hours = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CostCode = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Timesheets", x => x.TimesheetId);
                });

            migrationBuilder.CreateTable(
                name: "Valuations",
                columns: table => new
                {
                    ValuationId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ClaimPeriodId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    GrossValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RetentionPercent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsIssued = table.Column<bool>(type: "bit", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Valuations", x => x.ValuationId);
                });

            migrationBuilder.CreateTable(
                name: "VatAnalyses",
                columns: table => new
                {
                    VatAnalysisId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ZeroRatedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    StandardRatedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2048)", maxLength: 2048, nullable: false),
                    IsClientConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    IsArchitectConfirmed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VatAnalyses", x => x.VatAnalysisId);
                });

            migrationBuilder.CreateTable(
                name: "WalkRoundNotes",
                columns: table => new
                {
                    WalkRoundNoteId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AuthorEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: false),
                    PhotoCount = table.Column<int>(type: "int", nullable: false),
                    CapturedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WalkRoundNotes", x => x.WalkRoundNoteId);
                });

            migrationBuilder.CreateTable(
                name: "WorkOrders",
                columns: table => new
                {
                    WorkOrderId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ProjectId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BidPackageId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SubcontractorId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Scope = table.Column<string>(type: "nvarchar(1024)", maxLength: 1024, nullable: false),
                    AwardedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AwardedByEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOrders", x => x.WorkOrderId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccessRequests");

            migrationBuilder.DropTable(
                name: "BidDecisions");

            migrationBuilder.DropTable(
                name: "BidPackages");

            migrationBuilder.DropTable(
                name: "BoqLineItems");

            migrationBuilder.DropTable(
                name: "BoqSignOffs");

            migrationBuilder.DropTable(
                name: "CashflowSnapshots");

            migrationBuilder.DropTable(
                name: "ChangeRecords");

            migrationBuilder.DropTable(
                name: "ClaimPeriods");

            migrationBuilder.DropTable(
                name: "ComplianceDocuments");

            migrationBuilder.DropTable(
                name: "ContraCharges");

            migrationBuilder.DropTable(
                name: "CostCodeBudgets");

            migrationBuilder.DropTable(
                name: "CostCodes");

            migrationBuilder.DropTable(
                name: "CvrPackageRows");

            migrationBuilder.DropTable(
                name: "CvrSnapshots");

            migrationBuilder.DropTable(
                name: "Dayworks");

            migrationBuilder.DropTable(
                name: "Defects");

            migrationBuilder.DropTable(
                name: "DirectoryUserRoles");

            migrationBuilder.DropTable(
                name: "DirectoryUsers");

            migrationBuilder.DropTable(
                name: "DrawingIssueRecords");

            migrationBuilder.DropTable(
                name: "DrawingRevisions");

            migrationBuilder.DropTable(
                name: "Drawings");

            migrationBuilder.DropTable(
                name: "Eots");

            migrationBuilder.DropTable(
                name: "ForecastComponents");

            migrationBuilder.DropTable(
                name: "HandoverPackItems");

            migrationBuilder.DropTable(
                name: "HsRecordAttendance");

            migrationBuilder.DropTable(
                name: "HsRecords");

            migrationBuilder.DropTable(
                name: "InfoChaseItems");

            migrationBuilder.DropTable(
                name: "LeadOutcomes");

            migrationBuilder.DropTable(
                name: "Leads");

            migrationBuilder.DropTable(
                name: "MobilisationItems");

            migrationBuilder.DropTable(
                name: "Photos");

            migrationBuilder.DropTable(
                name: "PracticalCompletions");

            migrationBuilder.DropTable(
                name: "PrelimForecastEntries");

            migrationBuilder.DropTable(
                name: "PrelimItems");

            migrationBuilder.DropTable(
                name: "ProgrammeTasks");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Proposals");

            migrationBuilder.DropTable(
                name: "QsAccruals");

            migrationBuilder.DropTable(
                name: "QualificationAssessments");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "Rates");

            migrationBuilder.DropTable(
                name: "RetentionReleases");

            migrationBuilder.DropTable(
                name: "SettlementRecords");

            migrationBuilder.DropTable(
                name: "SiteReports");

            migrationBuilder.DropTable(
                name: "SiteVisits");

            migrationBuilder.DropTable(
                name: "SubcontractorRetentions");

            migrationBuilder.DropTable(
                name: "Subcontractors");

            migrationBuilder.DropTable(
                name: "Timesheets");

            migrationBuilder.DropTable(
                name: "Valuations");

            migrationBuilder.DropTable(
                name: "VatAnalyses");

            migrationBuilder.DropTable(
                name: "WalkRoundNotes");

            migrationBuilder.DropTable(
                name: "WorkOrders");
        }
    }
}
