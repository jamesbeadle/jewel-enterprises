using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.DocumentControl.Storage;
using Jewel.JPMS.Api.Features.ValuationInvoices.Commands;
using Jewel.JPMS.Api.Features.ValuationInvoices.XeroRaise;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.ValuationInvoices;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The accountant's ask (9 Sep 2026): Cert 15 had to be raised in Xero, tracked and have its PDF
/// attached by hand. Now the Issue step raises the AUTHORISED sales invoice on the client with the
/// project's Sites tracking, attaches the certificate the register holds, stamps Xero's number on
/// the valuation invoice and issues it — and refuses to do any of it twice. Then (10 Sep 2026,
/// Ravenswood Valuation 05): the contact is the one MAPPED ON THE PROJECT — never matched by name,
/// never created, a blocker when unmapped — the dates are the user's, the reference and description
/// number from the invoice, and a hand-raised Xero number is recorded on Issue or afterwards.
/// </summary>
public sealed class RaiseValuationInvoiceInXeroTests
{
    private const string InvoiceId = "VI-15";
    private const string ProjectId = "P-ABBOT";
    private const string ClaimId = "CLAIM-15";

    [Fact]
    public async Task Preview_showsTheClientTheNetTheSiteAndTheCertificate()
    {
        var fixture = await Fixture.CreateAsync(withCertificate: true);

        var preview = await fixture.PreviewAsync();

        Assert.True(preview.CanRaise, string.Join(" ", preview.Blockers));
        Assert.Equal("Quarry Developments Ltd", preview.ContactName);
        Assert.Equal("xero-contact-quarry", preview.XeroContactId);
        Assert.Contains("found in Xero", preview.ContactStatus);
        Assert.Equal(13703.94m, preview.Net);
        Assert.Equal("Abbot Road", preview.SiteOption);
        Assert.Equal("1986_7.03_260909 - Interim Certificate 15.pdf", preview.CertificateFileName);
        Assert.Equal(DateTime.UtcNow.Date, preview.Date);
        Assert.Equal(new DateTime(2026, 9, 3).AddDays(14), preview.DueDate);
        Assert.Contains("final date for payment", preview.DueDateNote);
        Assert.Contains("OUTPUT2", preview.TaxNote);
    }

    [Fact]
    public async Task Preview_blocksWhenNoXeroContactIsMappedOnTheProject_andNeverLooksUpByName()
    {
        var fixture = await Fixture.CreateAsync(withCertificate: true, xeroContactId: null);

        var preview = await fixture.PreviewAsync();

        Assert.False(preview.CanRaise);
        Assert.Null(preview.XeroContactId);
        Assert.Contains(preview.Blockers, blocker => blocker.Contains("No Xero contact is mapped on Abbot Road") && blocker.Contains("Project settings"));
        Assert.DoesNotContain(fixture.Xero.Calls, call => call.StartsWith("LookupSalesContact"));
        var refused = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.RaiseAsync());
        Assert.Contains("No Xero contact is mapped", refused.Message);
        Assert.Null(fixture.Xero.SalesInvoice);
    }

    [Fact]
    public async Task Preview_blocksWhenTheMappedContactIsNotInXero()
    {
        var fixture = await Fixture.CreateAsync(withCertificate: true, xeroContactId: "xero-contact-gone");

        var preview = await fixture.PreviewAsync();

        Assert.False(preview.CanRaise);
        Assert.Contains("LookupSalesContact:xero-contact-gone", fixture.Xero.Calls);
        Assert.Contains(preview.Blockers, blocker => blocker.Contains("not found in Xero") && blocker.Contains("re-map"));
        Assert.Contains("NOT found", preview.ContactStatus);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.RaiseAsync());
        Assert.Null(fixture.Xero.SalesInvoice);
    }

    [Fact]
    public async Task Dates_defaultToTodayAndTheCertificateRule_andTheUsersOverrideBoth()
    {
        var fixture = await Fixture.CreateAsync(withCertificate: true);

        var chosen = await fixture.PreviewAsync(new DateTime(2026, 9, 5, 14, 30, 0), new DateTime(2026, 10, 2));
        Assert.Equal(new DateTime(2026, 9, 5), chosen.Date);
        Assert.Equal(new DateTime(2026, 10, 2), chosen.DueDate);
        Assert.Contains("the date you gave", chosen.DueDateNote);

        var outcome = await fixture.RaiseAsync(new DateTime(2026, 9, 5), new DateTime(2026, 10, 2));
        var request = Assert.IsType<XeroSalesInvoiceRequest>(fixture.Xero.SalesInvoice);
        Assert.Equal(new DateTime(2026, 9, 5), request.Date);
        Assert.Equal(new DateTime(2026, 10, 2), request.DueDate);
        Assert.Equal(ValuationInvoiceStatus.Issued, outcome.Invoice.Status);

        var bare = await Fixture.CreateAsync(withCertificate: false);
        var defaulted = await bare.PreviewAsync();
        Assert.Equal(DateTime.UtcNow.Date, defaulted.Date);
        Assert.Null(defaulted.DueDate);
        Assert.Contains("Xero's default sales due date", defaulted.DueDateNote);
    }

    [Fact]
    public async Task ReferenceAndDescription_numberFromTheInvoice_neverTheClaim()
    {
        var fixture = await Fixture.CreateAsync(withCertificate: true);

        var preview = await fixture.PreviewAsync();
        await fixture.RaiseAsync();

        var request = Assert.IsType<XeroSalesInvoiceRequest>(fixture.Xero.SalesInvoice);
        Assert.Equal("Valuation 15", request.Reference);
        Assert.Equal("Valuation 15", preview.XeroReference);
        Assert.Equal("VI-0015", preview.Reference);
        Assert.Equal("Valuation 15 - Payment due as per September 2026 valuation report (ex VAT)", request.Description);
        Assert.Equal(request.Description, preview.Description);
        Assert.DoesNotContain("certificate", request.Description, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Interim Certificate 15.pdf", Assert.Single(fixture.Xero.Attached));

        var invoiceNine = await Fixture.CreateAsync(withCertificate: false, invoiceNumber: 9);
        Assert.Equal("Valuation 09", (await invoiceNine.PreviewAsync()).XeroReference);
    }

    [Fact]
    public async Task HandRaisedNumber_recordedOnIssue_countsAsRaised()
    {
        var fixture = await Fixture.CreateAsync(withCertificate: false);

        var issued = await new IssueValuationInvoiceHandler(fixture.Context)
            .HandleAsync(new IssueValuationInvoice(InvoiceId, "  INV-0227 "), CancellationToken.None);

        Assert.Equal(ValuationInvoiceStatus.Issued, issued.Status);
        Assert.Equal("INV-0227", issued.XeroInvoiceNumber);
        Assert.Null(issued.XeroInvoiceId);
        Assert.NotNull(issued.XeroRaisedAt);
        Assert.True(issued.IsRaisedInXero);
        Assert.False(issued.IsRaisedInXeroByPortal);

        var preview = await fixture.PreviewAsync();
        Assert.Contains(preview.Blockers, blocker => blocker.Contains("Already raised in Xero as INV-0227"));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.RaiseAsync());
        Assert.Null(fixture.Xero.SalesInvoice);
    }

    [Fact]
    public async Task RecordXeroNumber_backFillsAnIssuedInvoice_andRefusesOneThePortalRaised()
    {
        var fixture = await Fixture.CreateAsync(withCertificate: false);
        await new IssueValuationInvoiceHandler(fixture.Context).HandleAsync(new IssueValuationInvoice(InvoiceId), CancellationToken.None);
        var handler = new RecordValuationInvoiceXeroNumberHandler(fixture.Context);

        var recorded = await handler.HandleAsync(new RecordValuationInvoiceXeroNumber(InvoiceId, " INV-0205 ", "jeremy@jewelbb.co.uk"), CancellationToken.None);

        Assert.Equal("INV-0205", recorded.XeroInvoiceNumber);
        Assert.Null(recorded.XeroInvoiceId);
        Assert.NotNull(recorded.XeroRaisedAt);
        Assert.Equal(ValuationInvoiceStatus.Issued, recorded.Status);
        Assert.Contains(fixture.Context.ValuationInvoiceEvents,
            e => e.EventType == (int)ValuationInvoiceEventType.RaisedInXero && e.Note.Contains("INV-0205") && e.Note.Contains("by hand") && e.Note.Contains("jeremy@jewelbb.co.uk"));

        var portalRaised = await Fixture.CreateAsync(withCertificate: false);
        await portalRaised.RaiseAsync();
        var refused = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new RecordValuationInvoiceXeroNumberHandler(portalRaised.Context)
                .HandleAsync(new RecordValuationInvoiceXeroNumber(InvoiceId, "INV-0999"), CancellationToken.None));
        Assert.Contains("raised in Xero by the portal", refused.Message);

        Assert.True(new RecordValuationInvoiceXeroNumberValidation().Check(new RecordValuationInvoiceXeroNumber(InvoiceId, " ")).HasFailed);
        Assert.True(new RecordValuationInvoiceXeroNumberValidation().Check(new RecordValuationInvoiceXeroNumber(InvoiceId, new string('X', 65))).HasFailed);
    }

    [Fact]
    public async Task Raise_createsTheAuthorisedInvoice_attachesTheCertificate_stampsAndIssues()
    {
        var fixture = await Fixture.CreateAsync(withCertificate: true);

        var outcome = await fixture.RaiseAsync();

        var request = Assert.IsType<XeroSalesInvoiceRequest>(fixture.Xero.SalesInvoice);
        Assert.Equal("xero-contact-quarry", request.ContactId);
        Assert.Equal("Quarry Developments Ltd", request.ContactName);
        Assert.Equal(13703.94m, request.Net);
        Assert.Equal("Abbot Road", request.SiteOption);
        Assert.Equal("200", request.AccountCode);
        Assert.Equal("Valuation 15", request.Reference);
        Assert.Contains("Interim Certificate 15.pdf", Assert.Single(fixture.Xero.Attached));

        Assert.Equal("INV-0123", outcome.XeroInvoiceNumber);
        Assert.True(outcome.CertificateAttached);
        Assert.Equal(ValuationInvoiceStatus.Issued, outcome.Invoice.Status);
        Assert.Equal("INV-0123", outcome.Invoice.XeroInvoiceNumber);

        var stored = await fixture.Context.ValuationInvoices.SingleAsync(row => row.ValuationInvoiceId == InvoiceId);
        Assert.Equal("xero-sales-15", stored.XeroInvoiceId);
        Assert.Equal((int)ValuationInvoiceStatus.Issued, stored.Status);
        Assert.Contains(fixture.Context.ValuationInvoiceEvents,
            e => e.EventType == (int)ValuationInvoiceEventType.RaisedInXero && e.Note.Contains("INV-0123"));
    }

    [Fact]
    public async Task Raise_standsWithoutTheCertificate_andSaysSo()
    {
        var fixture = await Fixture.CreateAsync(withCertificate: true);
        fixture.Xero.AttachmentRefusal = "Xero rejected the attachment with HTTP 403 — the Xero custom connection needs the accounting.attachments scope";

        var outcome = await fixture.RaiseAsync();

        Assert.False(outcome.CertificateAttached);
        Assert.Contains("accounting.attachments", outcome.AttachmentError);
        Assert.Equal(ValuationInvoiceStatus.Issued, outcome.Invoice.Status);
        Assert.Equal("INV-0123", outcome.Invoice.XeroInvoiceNumber);
    }

    [Fact]
    public async Task Raise_refusesASecondTime_andAnUnmappedProject()
    {
        var fixture = await Fixture.CreateAsync(withCertificate: false);
        await fixture.RaiseAsync();

        var again = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.RaiseAsync());
        Assert.Contains("Already raised in Xero as INV-0123", again.Message);
        Assert.Single(fixture.Xero.Calls.Where(call => call == "CreateSalesInvoice"));

        var unmapped = await Fixture.CreateAsync(withCertificate: false, siteName: null);
        var preview = await unmapped.PreviewAsync();
        Assert.False(preview.CanRaise);
        Assert.Contains(preview.Blockers, blocker => blocker.Contains("no Xero site mapping"));
        var refused = await Assert.ThrowsAsync<InvalidOperationException>(() => unmapped.RaiseAsync());
        Assert.Contains("no Xero site mapping", refused.Message);
        Assert.Null(unmapped.Xero.SalesInvoice);
    }

    private sealed class Fixture
    {
        public JpmsContext Context { get; }
        public RecordingXero Xero { get; } = new() { RaisedSalesInvoiceId = "xero-sales-15", RaisedSalesInvoiceNumber = "INV-0123" };
        private readonly XeroOptions options = new();
        private readonly StubBlobs blobs = new();

        private Fixture(JpmsContext context) { Context = context; }

        public static async Task<Fixture> CreateAsync(
            bool withCertificate, string? siteName = "Abbot Road", string? xeroContactId = "xero-contact-quarry", int invoiceNumber = 15)
        {
            var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
                .UseInMemoryDatabase($"raise-in-xero-{Guid.NewGuid():N}").Options);
            context.Projects.Add(new ProjectEntity
            {
                ProjectId = ProjectId, Reference = "1986", Name = "Abbot Road", ClientName = "Quarry Developments Ltd", XeroSiteName = siteName,
                XeroContactId = xeroContactId, XeroContactName = xeroContactId is null ? null : "Quarry Developments Ltd"
            });
            // The claim's number is deliberately NOT the invoice's: Xero must be numbered from the invoice.
            context.ValuationClaims.Add(new ValuationClaimEntity { ValuationClaimId = ClaimId, ProjectId = ProjectId, ClaimNumber = 4, Name = "September 2026" });
            context.ValuationInvoices.Add(new ValuationInvoiceEntity
            {
                ValuationInvoiceId = InvoiceId, ProjectId = ProjectId, ValuationClaimId = ClaimId, Number = invoiceNumber, Reference = $"VI-{invoiceNumber:0000}",
                PeriodMonth = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), Amount = 13703.94m,
                Status = (int)ValuationInvoiceStatus.Approved, RaisedAt = DateTimeOffset.UtcNow
            });
            context.ValuationReportSnapshots.Add(new ValuationReportSnapshotEntity
            {
                ValuationReportSnapshotId = "SNAP-15", ProjectId = ProjectId, ValuationInvoiceId = InvoiceId, ValuationClaimId = ClaimId,
                Number = 15, Label = "VI-0015 raise", TakenAt = DateTimeOffset.UtcNow
            });
            context.ProjectContracts.Add(new ProjectContractEntity { ProjectContractId = "CONTRACT-1", ProjectId = ProjectId, FinalDateForPaymentDays = 14 });
            if (withCertificate)
                context.PaymentCertificates.Add(new PaymentCertificateEntity
                {
                    PaymentCertificateId = "CERT-15", ProjectId = ProjectId, CertificateNumber = "15", CertifiedAmount = 13703.94m,
                    IssuedDate = new DateTimeOffset(2026, 9, 3, 0, 0, 0, TimeSpan.Zero), ValuationClaimId = ClaimId,
                    FileName = "1986_7.03_260909 - Interim Certificate 15.pdf", ContentType = "application/pdf", BlobRef = "certs/15.pdf",
                    CreatedAt = DateTimeOffset.UtcNow, CreatedBy = "jeremy@jewelbb.co.uk"
                });
            await context.SaveChangesAsync();
            return new Fixture(context);
        }

        public Task<ValuationInvoiceXeroRaisePreview> PreviewAsync(DateTime? invoiceDate = null, DateTime? dueDate = null) =>
            new PreviewValuationInvoiceXeroRaiseHandler(Context, Xero, options)
                .HandleAsync(new PreviewValuationInvoiceXeroRaise(InvoiceId, invoiceDate, dueDate), CancellationToken.None);

        public Task<ValuationInvoiceXeroRaiseOutcome> RaiseAsync(DateTime? invoiceDate = null, DateTime? dueDate = null) =>
            new RaiseValuationInvoiceInXeroHandler(Context, Xero, options, blobs, new IssueValuationInvoiceHandler(Context))
                .HandleAsync(new RaiseValuationInvoiceInXero(InvoiceId, "jeremy@jewelbb.co.uk", invoiceDate, dueDate), CancellationToken.None);
    }

    /// <summary>The certificate's bytes, by blob ref — the register's own copy.</summary>
    private sealed class StubBlobs : IDocumentControlBlobStore
    {
        public Task<string> UploadItemAsync(string documentControlItemId, string fileName, string contentType, Stream content, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<string> UploadPaymentCertificateAsync(string projectId, string paymentCertificateId, string fileName, string contentType, Stream content, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<DocumentControlBlob?> OpenAsync(string blobRef, CancellationToken cancellationToken) =>
            Task.FromResult<DocumentControlBlob?>(new DocumentControlBlob(new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 }), "application/pdf", 4));
        public Task DeleteAsync(string blobRef, CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
