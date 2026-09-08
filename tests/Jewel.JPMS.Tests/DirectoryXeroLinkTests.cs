using Jewel.JPMS.Api.Data;
using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Audit;
using Jewel.JPMS.Api.Features.Subcontractors;
using Jewel.JPMS.Api.Features.Subcontractors.Commands;
using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Contracts.Xero;
using Jewel.JPMS.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The accountant's ask (8 Sep 2026): an existing directory record gets its Xero link in ONE
/// call, nothing else on the record moves, and no second record is minted. The acceptance case
/// is JP Air conditioning (on two work orders) ↔ JP Air Conditioning Services Ltd in Xero.
/// </summary>
public sealed class DirectoryXeroLinkTests
{
    private const string JpAirRecordId = "8a94b722029a4580bd01b8b84bd68589";
    private const string JpAirXeroContactId = "945eddf2-c1c2-4797-ab18-231e6c2f406d";

    [Fact]
    public async Task LinkingAnExistingRecordWritesTheLinkAndLeavesTheRecordAndItsWorkOrdersAlone()
    {
        var fixture = await Fixture.CreateAsync();

        var linked = await fixture.LinkAsync(JpAirRecordId, JpAirXeroContactId);

        Assert.True(linked.XeroLinked);
        Assert.Equal(JpAirXeroContactId, Assert.Single(linked.XeroLinks).XeroContactId);
        Assert.Equal("JP Air Conditioning Services Ltd", linked.XeroLinks[0].XeroContactName);
        Assert.Equal("accounts@jewelbb.co.uk", linked.XeroLinks[0].LinkedByEmail);
        Assert.Equal("JP Air conditioning", linked.CompanyName);

        Assert.Equal(1, await fixture.Context.Subcontractors.CountAsync());
        var link = Assert.Single(await fixture.Context.SubcontractorXeroLinks.ToListAsync());
        Assert.Equal((JpAirRecordId, JpAirXeroContactId), (link.SubcontractorId, link.XeroContactId));
        Assert.Equal(new[] { 31, 54 },
            await fixture.Context.WorkOrders.Where(order => order.SubcontractorId == JpAirRecordId).Select(order => order.Number).OrderBy(n => n).ToListAsync());

        var audit = Assert.Single(await fixture.Context.AuditEvents.ToListAsync());
        Assert.Equal((int)AuditEventType.DirectoryRecordXeroLinkChanged, audit.EventType);
        Assert.Equal("accounts@jewelbb.co.uk", audit.ActorEmail);
        Assert.Contains("JP Air conditioning linked to Xero contact JP Air Conditioning Services Ltd", audit.Detail);
    }

    [Fact]
    public async Task ARecordAlreadyLinkedIsRefusedNamingItsContact()
    {
        var fixture = await Fixture.CreateAsync();
        await fixture.LinkAsync(JpAirRecordId, JpAirXeroContactId);

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.LinkAsync(JpAirRecordId, "other-contact"));

        Assert.Contains("already linked to Xero contact JP Air Conditioning Services Ltd", refusal.Message);
        Assert.Equal(1, await fixture.Context.SubcontractorXeroLinks.CountAsync());
    }

    [Fact]
    public async Task AContactLinkedToAnotherRecordIsRefusedNamingThatRecord()
    {
        var fixture = await Fixture.CreateAsync();
        fixture.Context.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = "SUB-OTHER", CompanyName = "JP Air Conditioning Services Ltd" });
        await fixture.Context.SaveChangesAsync();
        await fixture.LinkAsync("SUB-OTHER", JpAirXeroContactId);

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.LinkAsync(JpAirRecordId, JpAirXeroContactId));

        Assert.Contains("already linked to JP Air Conditioning Services Ltd", refusal.Message);
    }

    [Fact]
    public async Task UnlinkingRemovesOnlyTheLinkAndIsAudited()
    {
        var fixture = await Fixture.CreateAsync();
        await fixture.LinkAsync(JpAirRecordId, JpAirXeroContactId);

        var unlinked = await fixture.UnlinkAsync(JpAirRecordId, JpAirXeroContactId);

        Assert.False(unlinked.XeroLinked);
        Assert.Empty(unlinked.XeroLinks);
        Assert.Equal(0, await fixture.Context.SubcontractorXeroLinks.CountAsync());
        Assert.Equal(1, await fixture.Context.Subcontractors.CountAsync());
        Assert.Equal(2, await fixture.Context.WorkOrders.CountAsync());
        Assert.Contains(await fixture.Context.AuditEvents.Select(row => row.Detail).ToListAsync(),
            detail => detail.Contains("unlinked from Xero contact JP Air Conditioning Services Ltd"));
    }

    [Fact]
    public async Task UnlinkingARecordThatIsNotLinkedIsRefused()
    {
        var fixture = await Fixture.CreateAsync();

        var refusal = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.UnlinkAsync(JpAirRecordId, JpAirXeroContactId));

        Assert.Contains("is not linked to that Xero contact", refusal.Message);
    }

    [Fact]
    public void TheDirectoryMatcherPairsJpAirWithItsXeroContactAndNothingElse()
    {
        var suppliers = new[]
        {
            Supplier(JpAirXeroContactId, "JP Air Conditioning Services Ltd"),
            Supplier("c-2", "JP Plumbing Ltd"),
            Supplier("c-3", "Air Conditioning Direct"),
        };

        var matches = DirectoryXeroMatcher.SuppliersMatching("JP Air conditioning", suppliers);

        Assert.Equal(JpAirXeroContactId, Assert.Single(matches).ContactId);
    }

    private static XeroSupplier Supplier(string contactId, string name) =>
        new(contactId, name, "", "", "", "", "", "", "", Array.Empty<XeroContactPerson>(), IsSupplier: true);

    private sealed class Fixture
    {
        public JpmsContext Context { get; }
        public RecordingXero Xero { get; } = new();
        private readonly AuditActor actor = new() { Email = "accounts@jewelbb.co.uk" };

        private Fixture(JpmsContext context) { Context = context; }

        public static async Task<Fixture> CreateAsync()
        {
            var context = new JpmsContext(new DbContextOptionsBuilder<JpmsContext>()
                .UseInMemoryDatabase($"directory-xero-link-{Guid.NewGuid():N}").Options);
            context.Subcontractors.Add(new SubcontractorEntity { SubcontractorId = JpAirRecordId, CompanyName = "JP Air conditioning", Category = (int)DirectoryCategory.Subcontractor });
            context.WorkOrders.Add(new WorkOrderEntity { WorkOrderId = "WO-A", ProjectId = "P-BYFRANCE", SubcontractorId = JpAirRecordId, Number = 31 });
            context.WorkOrders.Add(new WorkOrderEntity { WorkOrderId = "WO-B", ProjectId = "P-BYFRANCE", SubcontractorId = JpAirRecordId, Number = 54 });
            await context.SaveChangesAsync();
            var fixture = new Fixture(context);
            fixture.Xero.Suppliers.Add(Supplier(JpAirXeroContactId, "JP Air Conditioning Services Ltd"));
            return fixture;
        }

        public Task<Subcontractor> LinkAsync(string subcontractorId, string xeroContactId) =>
            new LinkDirectoryRecordToXeroContactHandler(Context, new XeroSupplierLookup(Xero), actor, Audit())
                .HandleAsync(new LinkDirectoryRecordToXeroContact(subcontractorId, xeroContactId), CancellationToken.None);

        public Task<Subcontractor> UnlinkAsync(string subcontractorId, string xeroContactId) =>
            new UnlinkDirectoryRecordFromXeroContactHandler(Context, Audit())
                .HandleAsync(new UnlinkDirectoryRecordFromXeroContact(subcontractorId, xeroContactId), CancellationToken.None);

        private AuditTrail Audit() => new(Context, actor, NullLogger<AuditTrail>.Instance);
    }
}
