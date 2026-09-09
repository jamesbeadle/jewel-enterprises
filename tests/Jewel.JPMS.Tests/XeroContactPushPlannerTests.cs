using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Subcontractors.XeroContacts;
using Jewel.JPMS.Api.Features.Xero;
using Jewel.JPMS.Contracts.Xero;
using Xunit;

namespace Jewel.JPMS.Tests;

/// <summary>
/// The accountant's conditions for the contact push (9 Sep 2026): the record's primary contact
/// goes to Xero's primary person, its other contacts replace Xero's additional people, an empty
/// side on the portal never clears Xero's, five is the limit, and names split on the first space.
/// </summary>
public sealed class XeroContactPushPlannerTests
{
    private static readonly XeroContactPeople XeroNow = new("c1", "Wilson Electric (Battersea) Ltd",
        new XeroContactPerson("Old Owner", "owner@wilson.example"),
        new[] { new XeroContactPerson("Someone Gone", "gone@wilson.example") });

    [Fact]
    public void ThePrimaryContactAndOtherContactsReplaceXerosPeople()
    {
        var record = new SubcontractorEntity { SubcontractorId = "s1", CompanyName = "Wilson Electric", ContactName = "Craig Taylor", ContactEmail = "craig@wilson.example" };
        var contacts = new[] { Contact("Dominic Reeve", "dom@wilson.example"), Contact("Aisha Khan", "") };

        var (after, warnings) = XeroContactPushPlanner.Plan(record, contacts, XeroNow);

        Assert.Equal(new XeroContactPerson("Craig Taylor", "craig@wilson.example"), after.PrimaryPerson);
        Assert.Equal(new[] { "Dominic Reeve", "Aisha Khan" }, after.AdditionalPersons.Select(person => person.Name));
        Assert.Empty(warnings);
    }

    [Fact]
    public void AnEmptyPortalSideLeavesXerosPeopleAlone()
    {
        var record = new SubcontractorEntity { SubcontractorId = "s1", CompanyName = "Wilson Electric", ContactName = "" };

        var (after, warnings) = XeroContactPushPlanner.Plan(record, Array.Empty<CompanyContactEntity>(), XeroNow);

        Assert.Equal(XeroNow.PrimaryPerson, after.PrimaryPerson);
        Assert.Equal(XeroNow.AdditionalPersons, after.AdditionalPersons);
        Assert.Equal(2, warnings.Count);
    }

    [Fact]
    public void MoreThanFiveOtherContactsAreCutToFiveWithTheRestNamed()
    {
        var record = new SubcontractorEntity { SubcontractorId = "s1", CompanyName = "Wilson Electric", ContactName = "Craig Taylor" };
        var contacts = Enumerable.Range(1, 7).Select(number => Contact($"Person {number}", "")).ToList();

        var (after, warnings) = XeroContactPushPlanner.Plan(record, contacts, XeroNow);

        Assert.Equal(5, after.AdditionalPersons.Count);
        Assert.Contains("Person 6, Person 7 will be left off", Assert.Single(warnings));
    }

    [Theory]
    [InlineData("Tom Dix", "Tom", "Dix")]
    [InlineData("Mary Anne Smith", "Mary", "Anne Smith")]
    [InlineData("Dominic", "Dominic", "")]
    public void ANameSplitsOnItsFirstSpace(string name, string firstName, string lastName) =>
        Assert.Equal((firstName, lastName), XeroClient.NameParts(name));

    private static CompanyContactEntity Contact(string name, string email) =>
        new() { CompanyContactId = Guid.NewGuid().ToString("N"), SubcontractorId = "s1", Name = name, Email = email, Phone = "" };
}
