using System.Text.Json.Nodes;
using Jewel.JPMS.Contracts.Xero;

namespace Jewel.JPMS.Api.Features.Xero;

public sealed partial class XeroClient
{
    public async Task<XeroContactPeople?> GetContactPeopleAsync(string contactId, CancellationToken ct)
    {
        if (!_options.IsConfigured) throw new XeroCallFailedException(NotConnected);
        var token = await GetAccessTokenAsync(ct);
        try
        {
            using var doc = await GetJsonAsync(token, $"{ContactsUrl}/{contactId}", "contact", ct);
            return FirstOf(doc, "Contacts") is { } contact ? ReadContactPeople(contact) : null;
        }
        catch (XeroCallFailedException failure) when (failure.Message.Contains("HTTP 404"))
        {
            return null;
        }
    }

    /// <summary>
    /// One POST to the contact: FirstName / LastName / EmailAddress for the primary person and
    /// the whole ContactPersons list. Xero replaces the list with what is sent, which is why the
    /// planner never sends an empty one. The cached supplier list is forgotten so the next
    /// directory read shows the people as written.
    /// </summary>
    public async Task<XeroApprovalResult> SetContactPeopleAsync(XeroContactPeople people, CancellationToken ct)
    {
        if (!_options.IsConfigured) return XeroApprovalResult.Failed(NotConnected);
        try
        {
            var token = await GetAccessTokenAsync(ct);
            var payload = new JsonObject { ["ContactID"] = people.ContactId };
            if (people.PrimaryPerson is { } primary)
            {
                var (firstName, lastName) = NameParts(primary.Name);
                payload["FirstName"] = firstName;
                payload["LastName"] = lastName;
                if (!string.IsNullOrWhiteSpace(primary.EmailAddress)) payload["EmailAddress"] = primary.EmailAddress;
            }
            payload["ContactPersons"] = ContactPersonsJson(people.AdditionalPersons);
            using var response = await SendJsonAsync(HttpMethod.Post, token, $"{ContactsUrl}/{people.ContactId}", payload, "update contact people", ct);
            _cachedSuppliers = null;
            return XeroApprovalResult.Ok("OK");
        }
        catch (XeroCallFailedException failure)
        {
            return XeroApprovalResult.Failed(failure.Message.Contains("HTTP 403")
                ? "Xero refused the write — the Cost Integration app needs the accounting.contacts scope, and the integration must be reconnected after it is added. " + failure.Message
                : failure.Message);
        }
    }

    private static XeroContactPeople ReadContactPeople(JsonElement contact)
    {
        var primaryName = FullNameOf(contact);
        var primaryEmail = StringOf(contact, "EmailAddress") ?? "";
        var primary = string.IsNullOrWhiteSpace(primaryName) ? null : new XeroContactPerson(primaryName, primaryEmail);
        return new XeroContactPeople(
            StringOf(contact, "ContactID") ?? "",
            StringOf(contact, "Name") ?? "",
            primary,
            ReadContactPersons(contact));
    }

    private static JsonArray ContactPersonsJson(IReadOnlyList<XeroContactPerson> persons)
    {
        var array = new JsonArray();
        foreach (var person in persons)
        {
            var (firstName, lastName) = NameParts(person.Name);
            array.Add(new JsonObject
            {
                ["FirstName"] = firstName,
                ["LastName"] = lastName,
                ["EmailAddress"] = person.EmailAddress,
                ["IncludeInEmails"] = true
            });
        }
        return array;
    }

    /// <summary>Xero holds a person as first name + last name; a portal name splits on its first space (agreed 2026-09-09).</summary>
    internal static (string FirstName, string LastName) NameParts(string name)
    {
        var trimmed = name.Trim();
        var space = trimmed.IndexOf(' ');
        return space < 0 ? (trimmed, "") : (trimmed[..space], trimmed[(space + 1)..].Trim());
    }
}
