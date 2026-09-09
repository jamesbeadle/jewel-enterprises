using Jewel.JPMS.Api.Data.Entities;

namespace Jewel.JPMS.Api.Features.Requests;

internal static class RequestsEntityMapping
{
    public static Request ToModel(this RequestEntity entity) => new(
        RequestId: entity.RequestId,
        ProjectId: entity.ProjectId,
        Kind: (RequestType)entity.Kind,
        Reference: entity.Reference,
        Title: entity.Title,
        Description: entity.Description,
        Status: (RequestStatus)entity.Status,
        Value: entity.Value,
        RaisedByEmail: entity.RaisedByEmail,
        RaisedAt: entity.RaisedAt,
        RespondedAt: entity.RespondedAt,
        ResponseText: entity.ResponseText,
        RespondedByEmail: entity.RespondedByEmail,
        ImpliesVariation: entity.ImpliesVariation,
        DrawingRef: entity.DrawingRef,
        ResponseDue: entity.ResponseDue,
        RelatedDrawingSpec: entity.RelatedDrawingSpec,
        InternalNotes: entity.InternalNotes,
        ClientNotes: entity.ClientNotes,
        Number: entity.Number,
        HasRfq: entity.HasRfq,
        PartyKind: (PartyKind)entity.PartyKind,
        PartyId: entity.PartyId,
        OnBehalfOfClientId: entity.OnBehalfOfClientId,
        BasisOfQueries: entity.BasisOfQueries,
        ResponseActionRequired: entity.ResponseActionRequired,
        ImpactIfLate: entity.ImpactIfLate,
        RelatedNodRequestId: entity.RelatedNodRequestId,
        MergedIntoRequestId: entity.MergedIntoRequestId,
        ClosedAt: entity.ClosedAt,
        IssuedAt: entity.IssuedAt,
        CriticalPath: entity.CriticalPath,
        CriticalPathNudgeDismissed: entity.CriticalPathNudgeDismissed,
        EotDaysClaimed: entity.EotDaysClaimed,
        EotDaysGranted: entity.EotDaysGranted);

    /// <summary>The model including its itemised queries (the numbered rows of the official document).</summary>
    public static Request ToModel(this RequestEntity entity, IEnumerable<RequestItemEntity> items) =>
        entity.ToModel() with
        {
            Items = items.OrderBy(i => i.Position).Select(i => i.ToModel()).ToList()
        };

    public static RequestItem ToModel(this RequestItemEntity entity) => new(
        RequestItemId: entity.RequestItemId,
        RequestId: entity.RequestId,
        Position: entity.Position,
        DrawingRef: entity.DrawingRef,
        MemberArea: entity.MemberArea,
        Query: entity.Query,
        Response: entity.Response);

    public static RequestMessage ToModel(this RequestMessageEntity entity) => new(
        MessageId: entity.MessageId,
        RequestId: entity.RequestId,
        AuthorEmail: entity.AuthorEmail,
        AuthorName: entity.AuthorName,
        Body: entity.Body,
        Visibility: (MessageVisibility)entity.Visibility,
        PostedAt: entity.PostedAt,
        Direction: (MessageDirection)entity.Direction,
        EmailMessageId: entity.EmailMessageId,
        InReplyTo: entity.InReplyTo,
        ConversationId: entity.ConversationId,
        SentStatus: (MessageSentStatus)entity.SentStatus,
        ParentMessageId: entity.ParentMessageId);

    // A mailbox email read live by tag, presented as one Shared leg of a request's conversation:
    // inbound when a correspondent sent it, outbound when the project mailbox itself did (sent
    // copies live in Sent Items — a sent message never arrives back in the Inbox, which is why the
    // mailbox-wide read exists at all). The body is the preview text (parity with the old
    // snapshot); full bodies are fetched on demand elsewhere. Never persisted — built fresh on
    // every read.
    public static RequestMessage ToConversationMessage(this MailboxMessage e, string requestId, string? mailboxAddress = null)
    {
        var outbound = !string.IsNullOrWhiteSpace(mailboxAddress)
            && string.Equals(e.FromEmail, mailboxAddress, StringComparison.OrdinalIgnoreCase);
        return new(
            MessageId: string.IsNullOrEmpty(e.InternetMessageId) ? e.Id : e.InternetMessageId,
            RequestId: requestId,
            AuthorEmail: e.FromEmail,
            AuthorName: string.IsNullOrWhiteSpace(e.FromName) ? e.FromEmail : e.FromName,
            Body: string.IsNullOrWhiteSpace(e.BodyPreview) ? "(no message body)" : e.BodyPreview,
            Visibility: MessageVisibility.Shared,
            PostedAt: e.ReceivedAt,
            Direction: outbound ? MessageDirection.Outbound : MessageDirection.Inbound,
            EmailMessageId: e.InternetMessageId,
            InReplyTo: null,
            ConversationId: null,
            // A live outbound copy exists in Sent Items, so it has demonstrably been sent.
            SentStatus: outbound ? MessageSentStatus.Sent : MessageSentStatus.NotApplicable,
            // Carried so the conversation view can expand the email to its full body on demand.
            MailboxId: e.Id,
            Subject: e.Subject);
    }
}
