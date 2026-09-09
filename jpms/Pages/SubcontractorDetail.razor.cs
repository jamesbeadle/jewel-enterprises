using Jewel.JPMS.Contracts.Subcontractors;
using Jewel.JPMS.Features.Directory;

namespace Jewel.JPMS.Pages;

public partial class SubcontractorDetail
{
    [Parameter] public string SubcontractorId { get; set; } = "";

    // Session checked and the user signed in. This is NOT "the record is here" — the directory's
    // own arrival is read from SubcontractorStore.IsLoaded above, so "not found" is never flashed
    // before the fetch lands.
    private Subcontractor? subcontractor;

    // Statement-of-account email modal (the download link needs no state).
    private bool statementOpen;

    // ---- Edit company details ----
    private bool editOpen;
    private bool editBusy;
    private string? editError;
    private string eCompany = "";
    private string eContact = "";
    private string eEmail = "";
    private string ePhone = "";
    private int eTermsDays = 30;
    private string eAddressLine = "";
    private string eTown = "";
    private string eCounty = "";
    private string ePostcode = "";

    private void OpenEdit()
    {
        if (subcontractor is null) return;
        eCompany = subcontractor.CompanyName;
        eContact = subcontractor.ContactName;
        eEmail = subcontractor.ContactEmail;
        ePhone = subcontractor.ContactPhone;
        eTermsDays = subcontractor.PaymentTermsDays;
        eAddressLine = subcontractor.AddressLine;
        eTown = subcontractor.Town;
        eCounty = subcontractor.County;
        ePostcode = subcontractor.Postcode;
        editError = null;
        editOpen = true;
    }

    private void CloseEdit() => editOpen = false;

    private async Task SaveEdit()
    {
        if (editBusy || subcontractor is null) return;
        if (string.IsNullOrWhiteSpace(eCompany)) { editError = "Company name can't be empty."; return; }
        if (eTermsDays is < 0 or > 365) { editError = "Payment terms must be between 0 and 365 days."; return; }
        editError = null;
        try
        {
            editBusy = true;
            await SubcontractorStore.UpdateDetailsAsync(
                subcontractor.SubcontractorId, eCompany, eContact, eEmail, ePhone, eTermsDays,
                eAddressLine, eTown, eCounty, ePostcode);
            editOpen = false;
        }
        catch (CommandFailedException ex) { editError = $"Couldn't save: {ex.Message}"; }
        catch { editError = "Couldn't save. Please try again."; }
        finally { editBusy = false; }
    }

    // Restricted to administrators, the managing and finance directors, and project managers —
    // the roles allowed to edit directory records (mirrors the API's UpdateSubcontractor gate).
    private bool CanAccess => Session.AvailableRoles.Any(r =>
        r is Role.Admin or Role.ManagingDirector or Role.FinanceDirector or Role.ProjectManager);

    // ---- Xero link ----

    // Linking decides which Xero supplier the company's bills reconcile against — the Directory
    // page's management gate (Admin, MD, FD), mirroring the API's link/import authorisation.
    private bool CanManageXeroLink => Session.AvailableRoles.Any(r =>
        r is Role.Admin or Role.ManagingDirector or Role.FinanceDirector);

    private XeroLinkModal? xeroLinkModal;
    private bool xeroBusy;
    private string? xeroError;

    private static string XeroLinkTitle(Subcontractor sub) =>
        sub.XeroLinks.Count == 0
            ? "This record holds a Xero link"
            : "Linked to Xero contact " + string.Join(" and ", sub.XeroLinks.Select(link => link.XeroContactName));

    private void OpenXeroLink()
    {
        if (subcontractor is null) return;
        xeroError = null;
        xeroLinkModal?.Open(subcontractor.SubcontractorId, subcontractor.CompanyName);
    }

    private async Task UnlinkFromXero(string xeroContactId)
    {
        if (xeroBusy || subcontractor is null) return;
        xeroError = null;
        try
        {
            xeroBusy = true;
            await SubcontractorStore.UnlinkFromXeroAsync(subcontractor.SubcontractorId, xeroContactId);
        }
        catch (CommandFailedException ex) { xeroError = $"Couldn't unlink: {ex.Message}"; }
        catch { xeroError = "Couldn't remove the Xero link. Please try again."; }
        finally { xeroBusy = false; }
    }

    private string PageTitleText => subcontractor?.CompanyName ?? "Subcontractor";

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!Auth.IsSignedIn) { Nav.NavigateTo("/login", forceLoad: true); return; }
        SubcontractorStore.OnChange += HandleChange;
        Reload();
    }

    protected override void OnParametersSet() => Reload();
    private void Reload()
    {
        subcontractor = SubcontractorStore.Find(SubcontractorId);
        // Kick off the contacts fetch: ContactsLoadedFor only reports arrival, this read is what
        // starts the one-time load (AsyncQueryCache), so the gate above can ever open.
        _ = SubcontractorStore.ContactsFor(SubcontractorId);
    }
    private void HandleChange() { Reload(); StateHasChanged(); }

    public void Dispose() => SubcontractorStore.OnChange -= HandleChange;

    private static string ContactLine(Subcontractor sub) =>
        string.Join(" · ", new[] { sub.TradesLabel, sub.ContactName, sub.ContactEmail }.Where(x => !string.IsNullOrWhiteSpace(x)));

    // ---- Portal access ----

    private bool inviteBusy;
    private string? inviteError;
    private Contracts.Auth.InviteResult? inviteResult;

    private async Task InviteToPortal()
    {
        if (inviteBusy || subcontractor is null) return;
        inviteError = null;
        try
        {
            inviteBusy = true;
            var outcome = await Invites.InviteSubcontractorAsync(subcontractor.SubcontractorId);
            if (outcome.Success) inviteResult = outcome.Result;
            else inviteError = outcome.Error;
        }
        finally { inviteBusy = false; }
    }

    // ---- Trade management ----

    private bool tradesBusy;
    private string? tradesError;
    private string? tradesNote;
    private string newTradeName = "";

    private async Task AddExistingTrade(ChangeEventArgs e)
    {
        var tradeId = e.Value?.ToString();
        if (string.IsNullOrEmpty(tradeId) || subcontractor is null || subcontractor.HasTrade(tradeId)) return;
        await SaveTrades(subcontractor.Trades.Select(t => t.TradeId).Append(tradeId).ToList());
    }

    private async Task RemoveTrade(string tradeId)
    {
        if (subcontractor is null) return;
        await SaveTrades(subcontractor.Trades.Where(t => !string.Equals(t.TradeId, tradeId, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.TradeId).ToList());
    }

    // Creates the trade in the curated list (or reuses it if the name already exists), then applies it.
    private async Task AddNewTrade()
    {
        if (tradesBusy || subcontractor is null || string.IsNullOrWhiteSpace(newTradeName)) return;
        tradesError = null; tradesNote = null;
        try
        {
            tradesBusy = true;
            var trade = await SubcontractorStore.AddTradeAsync(newTradeName.Trim());
            if (!subcontractor.HasTrade(trade.TradeId))
                await SubcontractorStore.SetTradesAsync(subcontractor.SubcontractorId,
                    subcontractor.Trades.Select(t => t.TradeId).Append(trade.TradeId).ToList());
            newTradeName = "";
            tradesNote = "Saved.";
        }
        catch (CommandFailedException ex) { tradesError = $"Couldn't update trades: {ex.Message}"; }
        catch { tradesError = "Couldn't update trades. Please try again."; }
        finally { tradesBusy = false; }
    }

    private async Task SaveTrades(IReadOnlyList<string> tradeIds)
    {
        if (tradesBusy || subcontractor is null) return;
        tradesError = null; tradesNote = null;
        try
        {
            tradesBusy = true;
            await SubcontractorStore.SetTradesAsync(subcontractor.SubcontractorId, tradeIds);
            tradesNote = "Saved.";
        }
        catch (CommandFailedException ex) { tradesError = $"Couldn't update trades: {ex.Message}"; }
        catch { tradesError = "Couldn't update trades. Please try again."; }
        finally { tradesBusy = false; }
    }

    // ---- Contacts ----
    // The record's additional people (beyond the primary contact line): consolidation lands the
    // merged records' details here, Xero imports land the Xero contact persons, and anyone can be
    // added by hand. One small form serves both add and edit — Edit loads the row into it.

    private bool contactBusy;
    private string? contactsError;
    private string? contactsNote;
    private string? editingContactId;
    private string cName = "", cEmail = "", cPhone = "";

    private IReadOnlyList<CompanyContact> Contacts =>
        subcontractor is null
            ? Array.Empty<CompanyContact>()
            : SubcontractorStore.ContactsFor(subcontractor.SubcontractorId);

    private void EditContact(CompanyContact contact)
    {
        editingContactId = contact.CompanyContactId;
        cName = contact.Name; cEmail = contact.Email; cPhone = contact.Phone;
    }

    private void CancelEditContact()
    {
        editingContactId = null;
        cName = cEmail = cPhone = "";
    }

    private async Task SaveContact()
    {
        if (contactBusy || subcontractor is null) return;
        if (string.IsNullOrWhiteSpace(cName) && string.IsNullOrWhiteSpace(cEmail) && string.IsNullOrWhiteSpace(cPhone))
        { contactsError = "A contact needs at least a name, an email address or a phone number."; return; }
        contactsError = null; contactsNote = null;
        try
        {
            contactBusy = true;
            await SubcontractorStore.UpsertContactAsync(new UpsertCompanyContact(
                subcontractor.SubcontractorId, cName.Trim(), cEmail.Trim(), cPhone.Trim(),
                editingContactId));
            CancelEditContact();
            contactsNote = "Saved.";
        }
        catch (CommandFailedException ex) { contactsError = $"Couldn't save the contact: {ex.Message}"; }
        catch { contactsError = "Couldn't save the contact. Please try again."; }
        finally { contactBusy = false; }
    }

    private async Task RemoveContact(string companyContactId)
    {
        if (contactBusy || subcontractor is null) return;
        contactsError = null; contactsNote = null;
        try
        {
            contactBusy = true;
            await SubcontractorStore.RemoveContactAsync(subcontractor.SubcontractorId, companyContactId);
            if (editingContactId == companyContactId) CancelEditContact();
        }
        catch (CommandFailedException ex) { contactsError = $"Couldn't remove the contact: {ex.Message}"; }
        catch { contactsError = "Couldn't remove the contact. Please try again."; }
        finally { contactBusy = false; }
    }

    private static string Dash(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
}
