// The boot screen: the jewel that fills the window from the first byte of HTML until the app has
// something real to show.
//
// It used to live INSIDE #app, which meant Blazor wiped it the instant it rendered its first
// frame — long before the session had been checked. The user watched a large centred jewel, then
// the chrome snapping in around a second, smaller jewel somewhere else on the page, then the
// page. Three states for one wait. Now the boot screen is a sibling overlay that stays up, and
// the app takes it down (BootScreen.DismissAsync, from ApprovedSessionGate, LandingLayout and
// ReportingErrorBoundary) at the moment it can replace it with content. The handover is a 200ms
// crossfade onto a screen that is already drawn.
//
// The failsafe matters more than the fade: a veil nobody removes is a dead app. Once Blazor has
// rendered anything at all into #app, a timer starts, and the boot screen goes whether or not the
// app remembered to dismiss it. It is deliberately longer than a cold start of the API: while the
// boot screen is up, the page gate's own jewel is drawn invisibly beneath it, and a failsafe that
// fires mid-session-check swaps one jewel for another — the very flicker this overlay exists to
// remove. Twelve seconds is past the slowest cold session check we see, and still short enough
// that a page which forgot to dismiss is usable rather than dead.
window.jpmsBoot = {
    FAILSAFE_MS: 12000,
    FADE_MS: 200,

    dismiss() {
        const boot = document.getElementById('boot');
        if (!boot || boot.dataset.going) return;
        boot.dataset.going = 'true';
        boot.style.transition = 'opacity ' + this.FADE_MS + 'ms ease';
        boot.style.opacity = '0';
        setTimeout(() => boot.remove(), this.FADE_MS);
    },

    // Boot itself failed — there is no app coming. The overlay stays: the ring stops, the jewel
    // holds still, and the #boot-failed template (index.html) replaces the "Loading" line.
    fail() {
        const boot = document.getElementById('boot');
        const failed = document.getElementById('boot-failed');
        if (!boot || !failed || boot.dataset.going) return;
        boot.querySelector('[data-boot-ring]')?.remove();
        boot.querySelector('[data-boot-jewel]')?.classList.remove('animate-jewel-pulse');
        boot.querySelector('[data-boot-message]')?.replaceWith(failed.content.cloneNode(true));
    }
};

(function () {
    const app = document.getElementById('app');
    if (!app) return;
    const armFailsafe = () => setTimeout(() => window.jpmsBoot.dismiss(), window.jpmsBoot.FAILSAFE_MS);
    if (app.childElementCount > 0) { armFailsafe(); return; }
    const watch = new MutationObserver(() => {
        if (app.childElementCount === 0) return;
        watch.disconnect();
        armFailsafe();
    });
    watch.observe(app, { childList: true });
})();
