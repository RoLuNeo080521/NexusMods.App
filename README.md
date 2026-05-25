> ## ⚠️ Unofficial Community Fork
>
> **This is an unofficial, community-maintained fork of the Nexus Mods App. It is _not_ affiliated with, endorsed by, or supported by Nexus Mods / Black Tree Gaming Ltd.**
>
> The original developers [paused development of the Nexus Mods App to refocus on Vortex](https://help.nexusmods.com/article/164-discontinuing-the-nexus-mods-app-faq). This fork exists to keep the **Linux** experience working for the community by maintaining bug fixes on top of their excellent work.
>
> ### Why this fork exists
>
> The upstream app had several Linux-specific issues that prevented it from working correctly. This fork addresses them so Linux users can continue managing their mods.
>
> ### Changes made in this fork
>
> - **Apply crash fix** — archive validation no longer aborts the whole operation when a file can't be backed up (handles it gracefully instead).
> - **`libX11` / XDG portal crash fix** — falls back to `xdg-open` when the desktop portal isn't available, so opening links/files no longer crashes the app.
> - **Case-sensitivity fix** — files are now resolved case-insensitively on Linux's case-sensitive filesystems (fixes mods whose filenames differ in case).
> - **Collection cyclic-dependency fix** — a circular load-order rule in a collection no longer blocks the entire install; the cycle is broken gracefully.
> - **Collection fallback installer** — unrecognized collection items (e.g. loose, non-archive files) are now installed into a default folder instead of failing the whole collection.
>
> See the commit history for the exact, line-by-line changes.
>
> ### Status & disclaimer
>
> This fork is provided **as-is**, on a best-effort basis, with no warranty. Some fixes (notably the collection-related ones) may not yet be fully validated in every scenario. Use at your own discretion.
>
> ### Credits
>
> All credit for the original application goes to the **Nexus Mods team** and the upstream contributors. This fork merely maintains community patches on top of their work, and is grateful for everything they built.
>
> ### License
>
> This project remains licensed under **GPL-3.0-only**, exactly as the original. All original copyright notices are preserved. The full license text is available in the [`LICENSE`](./LICENSE) file.
>
> ---
>
> _Everything below this line is the original, unmodified README from the upstream project._

---


<div align="center">
	<h1>The Nexus Mods app</h1>
	<img src="https://github.com/Nexus-Mods/NexusMods.MkDocsMaterial.Themes.Next/blob/2b49cf1fdd0f15684c6057259b52210e73705b98/Images/Nexus-Icon.png?raw=true" width="150" align="center" />
	<br/> <br/>
    Mod With Confidence
    <br/>
    The <i>future</i> of modding with <i>Nexus Mods</i>.
    <br/><br/>
</div>

Nexus Mods app is a mod installer, creator and manager for all your popular games.

Easy to use, runs on your standard Windows PC and Linux alike. Don't waste time troubleshooting, play your games,
fill those knees with arrows and most importantly, ***Have Fun***!

Learn more about the App on the [Wiki](https://nexus-mods.github.io/NexusMods.App/)
