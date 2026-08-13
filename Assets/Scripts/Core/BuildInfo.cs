namespace SpaceGame
{
    /// <summary>
    /// Which build you are actually running.
    ///
    /// The whole game is code, so "did my pull land?" is otherwise invisible
    /// until you go hunting for a feature. This stamp is printed in the HUD top
    /// bar and written to the message log on every boot, so a glance answers it.
    ///
    /// Bumped with every pushed change: if the HUD does not show the version
    /// below, Unity is running stale code — the pull did not land, or the editor
    /// has not recompiled (or would not, because of a compile error in the
    /// Console).
    /// </summary>
    public static class BuildInfo
    {
        public const string Version = "0.18.0";
        public const string Codename = "Local Industry";
        public const string Date = "2026-08-13";

        /// <summary>What landed in this build — the thing to go and look at.</summary>
        public const string Headline =
            "manufacturing draws on the station bay as well as your hold";

        /// <summary>"v0.11.0 Shipyards" — for the HUD top bar.</summary>
        public static string Short => "v" + Version + " " + Codename;

        /// <summary>The boot line, written to the message log every run.</summary>
        public static string BootLine
            => "Build " + Version + " \"" + Codename + "\" (" + Date + ") — " + Headline + ".";
    }
}
