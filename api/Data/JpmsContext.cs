using Jewel.JPMS.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jewel.JPMS.Api.Data;

public sealed class JpmsContext : DbContext
{
    public JpmsContext(DbContextOptions<JpmsContext> options) : base(options) { }

    public DbSet<ProjectEntity> Projects => Set<ProjectEntity>();
    public DbSet<LeadEntity> Leads => Set<LeadEntity>();
    public DbSet<BoqLineItemEntity> BoqLineItems => Set<BoqLineItemEntity>();
    public DbSet<RateEntity> Rates => Set<RateEntity>();
    public DbSet<DrawingEntity> Drawings => Set<DrawingEntity>();
    public DbSet<DrawingRevisionEntity> DrawingRevisions => Set<DrawingRevisionEntity>();
    public DbSet<SubcontractorEntity> Subcontractors => Set<SubcontractorEntity>();
    public DbSet<ComplianceDocumentEntity> ComplianceDocuments => Set<ComplianceDocumentEntity>();
    public DbSet<HsRecordEntity> HsRecords => Set<HsRecordEntity>();
    public DbSet<BidPackageEntity> BidPackages => Set<BidPackageEntity>();
    public DbSet<QuoteEntity> Quotes => Set<QuoteEntity>();
    public DbSet<WorkOrderEntity> WorkOrders => Set<WorkOrderEntity>();
    public DbSet<ChangeRecordEntity> ChangeRecords => Set<ChangeRecordEntity>();
    public DbSet<SiteReportEntity> SiteReports => Set<SiteReportEntity>();
    public DbSet<ProgrammeTaskEntity> ProgrammeTasks => Set<ProgrammeTaskEntity>();
    public DbSet<ValuationEntity> Valuations => Set<ValuationEntity>();
    public DbSet<CvrSnapshotEntity> CvrSnapshots => Set<CvrSnapshotEntity>();
    public DbSet<CostCodeBudgetEntity> CostCodeBudgets => Set<CostCodeBudgetEntity>();
    public DbSet<TimesheetEntity> Timesheets => Set<TimesheetEntity>();
    public DbSet<DefectEntity> Defects => Set<DefectEntity>();
}
