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
/// the valuation invoice and issues it — and refuses to do any of it twice.
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
        Assert.Equal(13703.94m, preview.Net);
        Assert.Equal("Abbot Road", preview.SiteOption);
        Assert.Equal("1986_7.03_260909 - Interim Certificate 15.pdf", preview.CertificateFileName);
        Assert.Equal(new DateTime(2026, 9, 3).AddDays(14), preview.DueDate);
        Assert.Contains("OUTPUT2", preview.TaxNote);
    }

    [Fact]
    public async Task Raise_createsTheAuthorisedInvoice_attachesTheCertificate_stampsAndIssues()
    {
        var fixture = await Fixture.CreateAsync(withCertificate: true);

        var outcome = await fixture.RaiseAsync();

        var request = Assert.IsType<XeroSalesInvoiceRequest>(fixture.Xero.SalesInvoice);
        Assert.Equal("Quarry Developments Ltd", request.ContactName);
        Assert.Equal(13703.94m, request.Net);
        Assert.Equal("Abbot Road", request.SiteOption);
        Assert.Equal("200", request.AccountCode);
        Assert.Equal("VI-0015", request.Reference);
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

        public static async Task<Fixture> CreateAsync(bool withCertificate, string? siteName = "Abbot Road")
        {
            var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
                .UseInMemoryDatabase($"raise-in-xero-{Guid.NewGuid():N}").Options);
            context.Projects.Add(new ProjectEntity { ProjectId = ProjectId, Reference = "1986", Name = "Abbot Road", ClientName = "Quarry Developments Ltd", XeroSiteName = siteName });
            context.ValuationClaims.Add(new ValuationClaimEntity { ValuationClaimId = ClaimId, ProjectId = ProjectId, ClaimNumber = 15, Name = "September 2026" });
            context.ValuationInvoices.Add(new ValuationInvoiceEntity
            {
                ValuationInvoiceId = InvoiceId, ProjectId = ProjectId, ValuationClaimId = ClaimId, Number = 15, Reference = "VI-0015",
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

        public Task<ValuationInvoiceXeroRaisePreview> PreviewAsync() =>
            new PreviewValuationInvoiceXeroRaiseHandler(Context, Xero, options)
                .HandleAsync(new PreviewValuationInvoiceXeroRaise(InvoiceId), CancellationToken.None);

        public Task<ValuationInvoiceXeroRaiseOutcome> RaiseAsync() =>
            new RaiseValuationInvoiceInXeroHandler(Context, Xero, options, blobs, new IssueValuationInvoiceHandler(Context))
                .HandleAsync(new RaiseValuationInvoiceInXero(InvoiceId, "jeremy@jewelbb.co.uk"), CancellationToken.None);
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
