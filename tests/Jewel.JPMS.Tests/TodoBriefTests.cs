using Jewel.JPMS.Api.Data.Entities;
using Jewel.JPMS.Api.Features.Todos;
using Jewel.JPMS.Contracts.RecordLinks;
using Jewel.JPMS.Models;
using Xunit;

namespace Jewel.JPMS.Tests;

// The to-do brief's reasoning (2026-09-10, the accountant's ask): what an item's wording says
// it is for, which project records it names or plausibly concerns, and the one line that says
// what clears it. Pinned on the Ravenswood board the ask was made from.
public sealed class TodoBriefTests
{
    private static readonly DateOnly Today = new(2026, 9, 10);

    private static LinkableRecord Record(RecordType type, string reference, string title, string status, bool active = true) =>
        new(type, reference.ToLowerInvariant(), "p", reference, reference, title, status, "", active);

    private static TodoItemEntity Item(string title, string? notes = null, DateTime? due = null, Role? role = Role.QuantitySurveyor, int number = 1) => new()
    {
        TodoItemId = $"todo-{number}", Number = number, ProjectId = "p", Title = title, Notes = notes ?? "",
        DueAt = due is null ? null : new DateTimeOffset(due.Value, TimeSpan.Zero),
        AssigneeRole = role is null ? null : (int)role,
        CreatedAt = new DateTimeOffset(2026, 8, 20, 0, 0, 0, TimeSpan.Zero),
    };

    private static readonly LinkableRecord[] Ravenswood =
    {
        Record(RecordType.Variation, "V3", "Additional excavation and disposal for new pad foundations", "Approved"),
        Record(RecordType.Variation, "V2", "Drainage and civil works variation", "Approved"),
        Record(RecordType.BidPackageInvite, "BPI-0056", "External Windows and Doors", "Draft"),
        Record(RecordType.BidPackageInvite, "BPI-0041", "Windows, Doors & Roof Lights", "Draft"),
        Record(RecordType.BidPackageInvite, "BPI-0044", "Plumbing Installation & Testing", "Inviting"),
        Record(RecordType.BidPackageInvite, "BPI-0040", "Floor Screeds & Base Preparations", "Draft"),
    };

    [Fact]
    public void Intent_readsTheVerbAndTheKindOfRecordTheWorkProduces()
    {
        var raise = TodoBrief.ReadIntent("raise water softner variation");
        Assert.Equal("raise", raise.Verb);
        Assert.Equal(RecordType.Variation, raise.Expects);
        Assert.Equal("water softner", raise.Subject);

        var chase = TodoBrief.ReadIntent("Chase Window Company for Quote");
        Assert.Equal("chase", chase.Verb);
        Assert.Equal(RecordType.BidPackageInvite, chase.Expects);

        // An ORDER with no noun is a work order — nothing else in the portal is "ordering".
        var order = TodoBrief.ReadIntent("Order radiators from BTU once confirmed spec");
        Assert.Equal("order", order.Verb);
        Assert.Equal(RecordType.WorkOrder, order.Expects);

        var plain = TodoBrief.ReadIntent("H&S Questionaire");
        Assert.Null(plain.Verb);
        Assert.Null(plain.Expects);
    }

    [Fact]
    public void Relate_findsTheRecordByItsWords_andSaysItIsInferred()
    {
        var related = TodoBrief.Relate(Item("Chase Window Company for Quote"), Ravenswood);

        Assert.Equal(2, related.Count);
        Assert.All(related, r => Assert.False(r.Named));
        // Two equal matches: the newer package first.
        Assert.Equal("BPI-0056", related[0].Record.Reference);
        Assert.Equal(new[] { "window" }, related[0].MatchedWords);
    }

    [Fact]
    public void Relate_keepsANamedReferenceAheadOfInference()
    {
        var related = TodoBrief.Relate(Item("Follow up on BPI-0044 plumbing quotes"), Ravenswood);

        Assert.True(related[0].Named);
        Assert.Equal("BPI-0044", related[0].Record.Reference);
    }

    [Fact]
    public void Relate_refusesAMatchOnOneShortWord()
    {
        // "floor" alone must not drag the screed package onto a wall/ceiling/floor build-up item.
        var related = TodoBrief.Relate(Item("Wall, ceiling and floor build up for Site Manager"), Ravenswood);
        Assert.Empty(related);
    }

    [Fact]
    public void NextStep_saysRaiseIt_whenNothingOfTheExpectedKindMatches()
    {
        var item = Item("raise water softner variation");
        var step = TodoBrief.NextStep(item, TodoBrief.ReadIntent(item.Title), null, TodoBrief.Relate(item, Ravenswood));

        Assert.Contains("No variation on this project matches \"water softner\"", step);
        Assert.Contains("raise it", step);
    }

    [Fact]
    public void NextStep_readsTheDraftPackage_asNothingToChaseYet()
    {
        var item = Item("Chase Window Company for Quote");
        var step = TodoBrief.NextStep(item, TodoBrief.ReadIntent(item.Title), null, TodoBrief.Relate(item, Ravenswood));

        Assert.StartsWith("BPI-0056", step);
        Assert.Contains("still Draft", step);
        Assert.Contains("Inferred", step);
    }

    [Fact]
    public void NextStep_prefersTheRecordTheItemIsAbout()
    {
        var item = Item("Chase Window Company for Quote");
        var about = Record(RecordType.BidPackageInvite, "BPI-0041", "Windows, Doors & Roof Lights", "Inviting");
        var step = TodoBrief.NextStep(item, TodoBrief.ReadIntent(item.Title), about, TodoBrief.Relate(item, Ravenswood));

        Assert.StartsWith("About BPI-0041 (Inviting)", step);
        Assert.DoesNotContain("Inferred", step);
    }

    [Fact]
    public void NextStep_callsAFinishedRecordProbablyDone()
    {
        var item = Item("raise drainage variation");
        var step = TodoBrief.NextStep(item, TodoBrief.ReadIntent(item.Title), null, TodoBrief.Relate(item, Ravenswood));

        Assert.StartsWith("V2", step);
        Assert.Contains("approved", step);
        Assert.Contains("looks done", step);
    }

    [Fact]
    public void Condition_isReadOffTheWording_andPutOnTheStep()
    {
        Assert.Equal("once confirmed spec", TodoBrief.Condition("Order radiators from BTU once confirmed spec"));
        Assert.Null(TodoBrief.Condition("raise water softner variation"));

        var item = Item("Order radiators from BTU once confirmed spec");
        var step = TodoBrief.NextStep(item, TodoBrief.ReadIntent(item.Title), null, TodoBrief.Relate(item, Ravenswood));
        Assert.EndsWith("(\"once confirmed spec\").", step);
    }

    [Fact]
    public void Signals_leadWithTheCalendar_thenOwnership_thenLinks()
    {
        var overdue = Item("Wall, ceiling and floor build up for Site Manager", due: new DateTime(2026, 8, 29), role: null);
        var signals = TodoBrief.Signals(overdue, Today, null, Array.Empty<TodoBrief.RelatedRecord>(), 0, null, null);

        Assert.Equal("Overdue by 12 days.", signals[0]);
        Assert.Equal("Unassigned — nobody owns it.", signals[1]);
        Assert.Contains(signals, s => s.StartsWith("Not linked to any record and names none"));
        Assert.Contains(signals, s => s.StartsWith("No activity for"));

        var today = Item("H&S Questionaire", due: new DateTime(2026, 9, 10));
        Assert.Equal("Due today.", TodoBrief.Signals(today, Today, null, Array.Empty<TodoBrief.RelatedRecord>(), 0, null, null)[0]);
        Assert.Equal(0, TodoBrief.DaysOverdue(today, Today));
        Assert.Null(TodoBrief.DaysOverdue(Item("no date"), Today));
    }

    [Fact]
    public void RelatedItems_pairTheOrderWithTheVariationItWaitsOn()
    {
        var order = Item("Order radiators from BTU once confirmed spec", number: 101);
        var variation = Item("Raise Radiator supply Variation", number: 135);
        var other = Item("raise water softner variation", number: 119);

        var related = TodoBrief.RelatedItems(order, new[] { order, variation, other });
        Assert.Single(related);
        Assert.Equal("TODO-0135", related[0].Reference);
    }
}
