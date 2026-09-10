
namespace Jewel.JPMS.Pages;

public partial class AiSkillsAdmin
{
    private bool dataFailed;
    private bool saving;
    private bool savedTick;
    private bool isNew;
    private bool referenceIsNew;

    // Null means no fetch has landed — never render a count from it (CLAUDE.md loading states).
    private IReadOnlyList<SkillSummary>? skills;
    private SkillDraft? editing;
    private ReferenceDraft? editingReference;
    private List<SkillReferenceDetail> references = new();
    private List<string> saveErrors = new();

    // Mirrors the API's SkillRoles.ManageSkills — the board plus administrators. Nav visibility is
    // not permission; the endpoint gates for real.
    private bool CanSee =>
        Session.ActiveRole is Role.Admin or Role.ManagingDirector or Role.FinanceDirector;

    protected override async Task OnInitializedAsync()
    {
        await Session.EnsureLoadedAsync();
        if (!CanSee) return;

        await LoadAsync();

        // Deep links from the AI Agents page land straight in the right editor:
        // ?skill=… opens that skill; ?agent=…&new=1 starts a new one under that discipline.
        // Parsed from the URL by hand, the same way ProjectRequestDetail reads ?openModal=.
        var skillParam = QueryValue("skill");
        if (!string.IsNullOrWhiteSpace(skillParam))
        {
            await OpenAsync(skillParam);
        }
        else if (!string.IsNullOrWhiteSpace(QueryValue("new")))
        {
            StartNew();
            var agentParam = QueryValue("agent");
            if (editing is not null && !string.IsNullOrWhiteSpace(agentParam))
                editing.AgentKey = agentParam.Trim().ToLowerInvariant();
        }
    }

    private string? QueryValue(string name)
    {
        var query = new Uri(Nav.Uri).Query.TrimStart('?');
        foreach (var pair in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = pair.IndexOf('=');
            if (separator <= 0) continue;
            if (!string.Equals(pair[..separator], name, StringComparison.OrdinalIgnoreCase)) continue;
            return Uri.UnescapeDataString(pair[(separator + 1)..]);
        }
        return null;
    }

    private async Task LoadAsync()
    {
        dataFailed = false;
        skills = null;
        try
        {
            skills = await Queries.AskAsync(new ListAiSkills(), CancellationToken.None);
        }
        catch
        {
            dataFailed = true;
        }
    }

    private void StartNew()
    {
        editing = new SkillDraft();
        references = new List<SkillReferenceDetail>();
        editingReference = null;
        isNew = true;
        savedTick = false;
        saveErrors.Clear();
    }

    private async Task OpenAsync(string skillKey)
    {
        savedTick = false;
        saveErrors.Clear();
        editingReference = null;
        try
        {
            var detail = await Queries.AskAsync(new GetAiSkill(skillKey), CancellationToken.None);
            if (detail is null) return;

            editing = new SkillDraft
            {
                SkillKey = detail.SkillKey,
                AgentKey = detail.AgentKey,
                DisplayName = detail.DisplayName,
                Description = detail.Description,
                Body = detail.Body,
                Pinned = detail.Pinned,
                IsActive = detail.IsActive
            };
            references = detail.References.ToList();
            isNew = false;
        }
        catch
        {
            // The error toast carries the reference; staying on the list is the right failure mode.
        }
    }

    private void CloseEditor()
    {
        editing = null;
        editingReference = null;
        _ = LoadAsync();
    }

    private async Task SaveAsync()
    {
        if (editing is null || saving) return;

        saving = true;
        savedTick = false;
        saveErrors.Clear();
        try
        {
            await Commands.SendAsync(
                new SaveAiSkill(
                    editing.SkillKey.Trim(),
                    editing.AgentKey,
                    editing.DisplayName.Trim(),
                    editing.Description.Trim(),
                    editing.Body,
                    editing.Pinned,
                    editing.IsActive,
                    string.Empty),
                CancellationToken.None);

            savedTick = true;
            isNew = false;
        }
        catch (CommandFailedException failure)
        {
            // Validation answers come back verbatim — "a description is required" belongs next to
            // the form, not in the global toast.
            saveErrors = new List<string> { failure.Message };
        }
        catch
        {
            saveErrors = new List<string> { "The skill could not be saved. The error toast carries the reference." };
        }
        finally
        {
            saving = false;
        }
    }

    private void StartNewReference()
    {
        editingReference = new ReferenceDraft();
        referenceIsNew = true;
    }

    private void EditReference(SkillReferenceDetail reference)
    {
        editingReference = new ReferenceDraft
        {
            RefKey = reference.RefKey,
            DisplayName = reference.DisplayName,
            Description = reference.Description,
            Body = reference.Body
        };
        referenceIsNew = false;
    }

    private async Task SaveReferenceAsync()
    {
        if (editing is null || editingReference is null || saving) return;

        saving = true;
        saveErrors.Clear();
        try
        {
            await Commands.SendAsync(
                new SaveAiSkillReference(
                    editing.SkillKey.Trim(),
                    editingReference.RefKey.Trim(),
                    editingReference.DisplayName.Trim(),
                    editingReference.Description.Trim(),
                    editingReference.Body,
                    string.Empty),
                CancellationToken.None);

            editingReference = null;
            await OpenAsync(editing.SkillKey.Trim());
        }
        catch (CommandFailedException failure)
        {
            saveErrors = new List<string> { failure.Message };
        }
        catch
        {
            saveErrors = new List<string> { "The reference could not be saved. The error toast carries the reference." };
        }
        finally
        {
            saving = false;
        }
    }

    /// <summary>
    /// A pasted SKILL.md fills the form: when the body starts with a ---frontmatter--- block, its
    /// name/description lift into the fields (only where they are still empty, so a deliberate edit
    /// is never overwritten) and the frontmatter itself stays in the body — it is part of the file
    /// Nigel maintains, and round-tripping it out would make his next paste a diff nightmare.
    /// </summary>
    private void OnBodyInput(ChangeEventArgs args)
    {
        if (editing is null) return;
        var body = args.Value?.ToString() ?? "";
        editing.Body = body;

        if (!body.StartsWith("---", StringComparison.Ordinal)) return;
        var end = body.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (end < 0) return;

        foreach (var line in body[3..end].Split('\n'))
        {
            var separator = line.IndexOf(':');
            if (separator <= 0) continue;
            var name = line[..separator].Trim().ToLowerInvariant();
            var value = line[(separator + 1)..].Trim().Trim('"', '\'');
            if (value.Length == 0) continue;

            if (name == "name" && string.IsNullOrWhiteSpace(editing.SkillKey) && isNew)
                editing.SkillKey = value;
            if (name == "name" && string.IsNullOrWhiteSpace(editing.DisplayName))
                editing.DisplayName = value;
            if (name == "description" && string.IsNullOrWhiteSpace(editing.Description))
                editing.Description = value;
        }
    }

    private static string AgentLabel(string agentKey) => SkillDisciplines.LabelFor(agentKey);

    private static string FormatSize(int characters) =>
        characters >= 10_000 ? $"{characters / 1000}k chars" : $"{characters:N0} chars";

    /// <summary>Mutable editor state — the contracts records stay immutable.</summary>
    private sealed class SkillDraft
    {
        public string SkillKey { get; set; } = "";
        public string AgentKey { get; set; } = "shared";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Body { get; set; } = "";
        public bool Pinned { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }

    private sealed class ReferenceDraft
    {
        public string RefKey { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Description { get; set; } = "";
        public string Body { get; set; } = "";
    }
}
