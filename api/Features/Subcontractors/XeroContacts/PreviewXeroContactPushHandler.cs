using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Subcontractors;

namespace Jewel.JPMS.Api.Features.Subcontractors.XeroContacts;

/// <summary>What the push would write, for the person to confirm: Xero's people now and after.</summary>
public sealed class PreviewXeroContactPushHandler : IQueryHandler<PreviewXeroContactPush, XeroContactPushPreview>
{
    private readonly JpmsContext context;
    private readonly IXeroClient xero;

    public PreviewXeroContactPushHandler(JpmsContext context, IXeroClient xero) { this.context = context; this.xero = xero; }

    public async Task<XeroContactPushPreview> HandleAsync(PreviewXeroContactPush query, CancellationToken cancellationToken)
    {
        var push = await XeroContactPushContext.LoadAsync(context, xero, query.SubcontractorId, cancellationToken);
        var (after, warnings) = XeroContactPushPlanner.Plan(push.Record, push.Contacts, push.Now);
        return new XeroContactPushPreview(push.Now, after, warnings);
    }
}
