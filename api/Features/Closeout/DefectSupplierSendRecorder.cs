using Jewel.JPMS.Api.Features.MailboxIntake.Graph;

namespace Jewel.JPMS.Api.Features.Closeout;

// The bridge from the mailbox compose handler to a defect: when an email is SENT carrying a
// defect's "JPMS/DEF-####" tag (a new email from the defect's page is stamped with it; a reply in
// the defect's thread inherits it) AND one of its To addresses is the defect's supplier, the
// defect records its first SentToSupplierAt/By and, if still Open, moves to In progress — "raised
// with the supplier" is a fact the page and the register can show, not something the sender has
// to tick. Later sends to the same supplier are chases: visible in the correspondence, no stamp.
//
// Best-effort by design, like the to-do activity bridge: the email has already gone, so a
// failure here is logged and swallowed — it must never turn a sent email into an error.
public sealed class DefectSupplierSendRecorder
{
    private readonly JpmsContext context;
    private readonly ILogger<DefectSupplierSendRecorder> logger;

    public DefectSupplierSendRecorder(JpmsContext context, ILogger<DefectSupplierSendRecorder> logger)
    {
        this.context = context;
        this.logger = logger;
    }

    public async Task RecordSentAsync(
        IReadOnlyList<string> workflowTags, IReadOnlyList<string> to, string actorEmail, CancellationToken cancellationToken)
    {
        var numbers = DefectTagNumbers(workflowTags);
        if (numbers.Count == 0 || to.Count == 0) return;
        try
        {
            var defects = await context.Defects
                .Where(defect => numbers.Contains(defect.Number) && defect.SentToSupplierAt == null)
                .ToListAsync(cancellationToken);
            if (defects.Count == 0) return;

            var suppliers = await DefectSupplierLookup.ForAsync(context, defects, cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var changed = false;
            foreach (var defect in defects)
            {
                var supplierEmail = defect.SubcontractorId is { } id && suppliers.TryGetValue(id, out var supplier)
                    && !string.IsNullOrWhiteSpace(supplier.ContactEmail)
                    ? supplier.ContactEmail
                    : defect.AssignedToEmail;
                if (string.IsNullOrWhiteSpace(supplierEmail)) continue;
                if (!to.Any(address => string.Equals(address.Trim(), supplierEmail.Trim(), StringComparison.OrdinalIgnoreCase))) continue;

                defect.SentToSupplierAt = now;
                defect.SentToSupplierByEmail = actorEmail;
                if ((DefectStatus)defect.Status == DefectStatus.Open) defect.Status = (int)DefectStatus.InProgress;
                changed = true;
            }
            if (changed) await context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "A sent email could not be recorded against its defect(s).");
        }
    }

    // "JPMS/DEF-0012" → 12. Any tag that isn't a defect tag is ignored.
    public static IReadOnlyList<int> DefectTagNumbers(IEnumerable<string> workflowTags)
    {
        const string defectStem = TriageCategories.WorkflowPrefix + "DEF-";
        var numbers = new List<int>();
        foreach (var tag in workflowTags)
        {
            if (!tag.StartsWith(defectStem, StringComparison.OrdinalIgnoreCase)) continue;
            if (int.TryParse(tag[defectStem.Length..], out var number)) numbers.Add(number);
        }
        return numbers;
    }
}
