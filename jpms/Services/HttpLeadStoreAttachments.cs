using Jewel.JPMS.Models;

namespace Jewel.JPMS.Services;

public sealed partial class HttpLeadStore
{
    private readonly Dictionary<string, QualificationAssessment?> qualificationsByLead = new();
    private readonly Dictionary<string, IReadOnlyList<SiteVisit>> siteVisitsByLead = new();
    private readonly Dictionary<string, IReadOnlyList<InfoChaseItem>> infoChaseByLead = new();
    private readonly Dictionary<string, BidDecision?> bidDecisionsByLead = new();
    private readonly Dictionary<string, Proposal?> proposalsByLead = new();
    private readonly Dictionary<string, LeadOutcome?> outcomesByLead = new();

    public QualificationAssessment? GetQualification(string leadId) =>
        CachedScalar(qualificationsByLead, leadId, $"/api/leads/{leadId}/qualification");

    public void SaveQualification(QualificationAssessment assessment) =>
        _ = PostScalarAsync("/api/leads/qualifications", assessment, assessment.LeadId,
            qualificationsByLead, $"/api/leads/{assessment.LeadId}/qualification");

    public IReadOnlyList<SiteVisit> SiteVisitsFor(string leadId) =>
        CachedList(siteVisitsByLead, leadId, $"/api/leads/{leadId}/site-visits");

    public void SaveSiteVisit(SiteVisit visit) =>
        _ = PostListAsync("/api/leads/site-visits", visit, visit.LeadId,
            siteVisitsByLead, $"/api/leads/{visit.LeadId}/site-visits");

    public IReadOnlyList<InfoChaseItem> InfoChaseFor(string leadId) =>
        CachedList(infoChaseByLead, leadId, $"/api/leads/{leadId}/info-chase");

    public void SaveInfoChaseItem(InfoChaseItem item) =>
        _ = PostListAsync("/api/leads/info-chase", item, item.LeadId,
            infoChaseByLead, $"/api/leads/{item.LeadId}/info-chase");

    public BidDecision? GetBidDecision(string leadId) =>
        CachedScalar(bidDecisionsByLead, leadId, $"/api/leads/{leadId}/bid-decision");

    public void SaveBidDecision(BidDecision decision) =>
        _ = PostScalarAsync("/api/leads/bid-decisions", decision, decision.LeadId,
            bidDecisionsByLead, $"/api/leads/{decision.LeadId}/bid-decision");

    public Proposal? GetProposal(string leadId) =>
        CachedScalar(proposalsByLead, leadId, $"/api/leads/{leadId}/proposal");

    public void SaveProposal(Proposal proposal) =>
        _ = PostScalarAsync("/api/leads/proposals", proposal, proposal.LeadId,
            proposalsByLead, $"/api/leads/{proposal.LeadId}/proposal");

    public LeadOutcome? GetOutcome(string leadId) =>
        CachedScalar(outcomesByLead, leadId, $"/api/leads/{leadId}/outcome");

    public void SaveOutcome(LeadOutcome outcome) =>
        _ = PostScalarAsync("/api/leads/outcomes", outcome, outcome.LeadId,
            outcomesByLead, $"/api/leads/{outcome.LeadId}/outcome");
}
