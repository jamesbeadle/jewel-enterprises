using Microsoft.AspNetCore.Components.Rendering;

namespace Jewel.JPMS.Components;

/// <summary>
/// Catches exceptions thrown inside a page and turns them into a report rather than a dead app.
///
/// Blazor's stock ErrorBoundary renders a message and stops; the useful part — what actually went
/// wrong — never reaches anybody who could act on it. This one hands the exception to
/// <see cref="ErrorReporter"/>, so the full-width toast carries the reference and the stack, and
/// offers the user a way back onto the page without losing their session.
/// </summary>
public sealed class ReportingErrorBoundary : ErrorBoundaryBase
{
    [Inject] private ErrorReporter Reporter { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    /// <summary>
    /// Clear the error and re-render the page. ErrorBoundaryBase.Recover() is protected, so this is
    /// the way in for the router, which resets the boundary on every navigation — otherwise one
    /// broken page would keep showing its error screen on every page the user visited afterwards.
    /// </summary>
    public void Reset() => Recover();

    /// <summary>
    /// Report, then make sure the screen below is visible. On a cold load the failure can happen
    /// while the boot overlay is still up — the session check itself throwing, say — and nothing
    /// else will take it down, so the user would watch the jewel pulse over a fully drawn error
    /// screen until the failsafe fired.
    /// </summary>
    protected override async Task OnErrorAsync(Exception exception)
    {
        Reporter.ReportUnhandled(exception, "Page render");
        await BootScreen.DismissAsync(Js);
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (CurrentException is null)
        {
            builder.AddContent(0, ChildContent);
            return;
        }

        if (ErrorContent is not null)
        {
            builder.AddContent(1, ErrorContent(CurrentException));
            return;
        }

        // This screen carries the detail ITSELF rather than pointing at the error toast.
        //
        // The toast lives inside MainLayout, and the layout is rendered by the Router — which sits
        // INSIDE this boundary (see App.razor). So the moment the boundary trips it replaces the
        // layout, taking the toast with it: the old wording told the user to read a red bar that
        // this very screen had just removed from the page. Every render failure was therefore
        // reported as "something went wrong" with nothing to send on — the exact dead end this
        // component exists to prevent.
        var report = Reporter.Current;

        builder.OpenElement(2, "section");
        builder.AddAttribute(3, "class", "px-6 md:px-8 py-16");

        builder.OpenElement(4, "div");
        builder.AddAttribute(5, "class", "max-w-2xl mx-auto text-center");

        builder.OpenElement(6, "h1");
        builder.AddAttribute(7, "class", "text-2xl font-semibold text-content mb-2");
        builder.AddContent(8, "This page couldn't finish loading");
        builder.CloseElement();

        builder.OpenElement(9, "p");
        builder.AddAttribute(10, "class", "text-content-muted mb-6 max-w-md mx-auto leading-relaxed");
        builder.AddContent(11, "Copy the details below and send them on, then try again. Nothing you had "
                             + "already saved is affected.");
        builder.CloseElement();

        builder.OpenElement(12, "button");
        builder.AddAttribute(13, "type", "button");
        builder.AddAttribute(14, "class", "btn-primary");
        builder.AddAttribute(15, "onclick", EventCallback.Factory.Create(this, Recover));
        builder.AddContent(16, "Try again");
        builder.CloseElement();

        builder.CloseElement(); // centred block

        if (report is not null)
        {
            builder.OpenElement(17, "div");
            builder.AddAttribute(18, "class",
                "max-w-2xl mx-auto mt-8 rounded border border-line bg-surface-raised overflow-hidden text-left");

            builder.OpenElement(19, "div");
            builder.AddAttribute(20, "class",
                "px-4 py-2 border-b border-line flex items-center justify-between gap-3");

            builder.OpenElement(21, "span");
            builder.AddAttribute(22, "class", "font-mono text-sm font-semibold text-content");
            builder.AddContent(23, report.Reference);
            builder.CloseElement();

            builder.OpenElement(24, "span");
            builder.AddAttribute(25, "class", "text-xs text-content-subtle");
            builder.AddContent(26, report.OccurredAt.ToLocalTime().ToString("dd MMM yyyy, HH:mm:ss"));
            builder.CloseElement();

            builder.CloseElement(); // header

            // Selectable plain text — the same block the toast's Copy button produces, so someone
            // stuck on this screen can select all and paste it into an email with nothing lost.
            builder.OpenElement(27, "pre");
            builder.AddAttribute(28, "class",
                "px-4 py-3 text-xs text-content-muted whitespace-pre-wrap break-words max-h-80 overflow-y-auto");
            builder.AddContent(29, report.ToPlainText());
            builder.CloseElement();

            builder.CloseElement(); // detail block
        }

        builder.CloseElement(); // section
    }
}
