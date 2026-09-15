# Icod.DCurses

![Icod TUI Toolchain](https://raw.githubusercontent.com/uniblab/Icod.DCurses/v0.8.0/icod_tui_toolchain.jpg)

[![PR Staging build](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml/badge.svg)](https://github.com/uniblab/Icod.DCurses/actions/workflows/pull-request.yaml)
[![Main Release validation](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml/badge.svg?branch=main)](https://github.com/uniblab/Icod.DCurses/actions/workflows/main.yaml)

`Icod.DCurses` is a managed, cross-platform curses-style terminal UI library for .NET.

It sits above `Icod.Terminal` and `Icod.TermInfo`:

- `Icod.TermInfo` owns immutable terminal capability descriptions and expansion;
- `Icod.Terminal` owns the live terminal session, host mode, dimensions, lifecycle, input decoding, semantic terminal protocols, physical pointer protocol/state, and output serialization;
- `Icod.DCurses` owns curses-shaped events, logical screens/windows, pads/viewports, retained panels/layers, cells/styles/metadata, composition, retained refresh policy, terminal-cell layout primitives, interaction regions/scopes, logical focus, pointer capture, pointer-gesture/command routing, and pointer-shape preferences.

## Status

Stable-source release: `Icod.DCurses 1.5.0`.

`Icod.DCurses 1.5.0` is complete in stable-source form in PR #30. The unchanged RC head `23113b130d674da315ccbbcd384a60a0e6b47baa` passed workflow #899 / `34993884108` across the complete seven-job Staging matrix. Stable-source head `af30a6c2df842d76b8cb9e9b7855aeb3a16e9ff9` passed workflow #905 / `34994761777`, and the final evidence-only PR head `269c595678563cc031be3372a5560c29101eafb8` passed workflow #909 / `34995361106`, each across package candidate plus Windows/Linux/macOS x64/ARM64. Merge, tagging, and publication remain separate maintainer actions.

Current source/package identity:

```text
Version         1.5.0
PackageVersion  1.5.0
AssemblyVersion 1.0.0.0
Icod.Terminal   1.15.0
Icod.TermInfo   1.14.0
```

Published 1.4 contract:

```text
62 exported types
491 canonical declared contract lines
sha256 8afe72deaa5354ee072de8ae17b04d8a1a0a8f730d5e3a737b4a47a539379147
```

Frozen 1.5 contract:

```text
69 exported types
525 canonical declared contract lines
sha256 8807aa15714b0b059f2aaa5ef1ff33bce3ed0bfc44455d8a352ee7ecff8313c0
```

Version 1.4 established deterministic interaction routing: bounded interaction regions, panel-aware hit testing, logical focus/traversal/repair, semantic key gestures and command identities, structured routing results, pointer-shape preferences, and a DCurses pointer-shape lease wrapper over Terminal-owned state.

Version 1.5 extends that mechanism with bounded interaction scopes, explicit singular pointer capture, deterministic spatial logical focus, clock-free pointer gesture normalization, and scope-level command bindings. It does not add widgets, callbacks, automatic focus-on-click, drag/drop policy, a retained event tree, or a hidden application event loop.

## Support the Project

`Icod.Terminal` and its ecosystem packages (`Icod.TermInfo` and `Icod.DCurses`) are built and maintained by a solo developer. If these packages save you or your team time, please consider supporting their continued development and maintenance.

[![GitHub Sponsors](https://img.shields.io/badge/GitHub-Sponsor?logo=githubsponsors)](https://github.com/sponsors/uniblab)
[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support?logo=kofi)](https://ko-fi.com/TimothyBruce)
[![PayPal](https://img.shields.io/badge/PayPal-Support?logo=paypal)](https://paypal.me/uniblab)

## Installation

Install the latest published stable package selected by your normal NuGet policy:

```text
dotnet add package Icod.DCurses
```

To install this release explicitly:

```text
dotnet add package Icod.DCurses --version 1.5.0
```

Source qualification and package publication are separate operations. The NuGet package page and GitHub Releases page are authoritative for distribution availability.

## Architecture

```text
applications / future widgets / compatibility facades
                         |
                    Icod.DCurses
 windows / pads / panels / cells / semantic metadata
 immutable geometry / pure layout / retained refresh / events
 interaction regions / scopes / logical + spatial focus
 capture / pointer gestures / gesture-command routing
              pointer-shape preferences
                         |
                    Icod.Terminal
   live session / input / lifecycle / semantic protocols
 physical pointer state / capability routing / serialized output
                         |
                    Icod.TermInfo
             immutable capability authority
                         |
                  terminal / tty
```

`Icod.DCurses` does not maintain a second terminal capability database, install a competing raw-input loop, own terminal modes independently of `Icod.Terminal`, emit private OSC/CSI/DCS/APC framing for Terminal-owned protocols, emulate a terminal, or create/manage PTYs.

Versions 1.4 and 1.5 also do not add a widget framework, hidden event loop, callback dispatcher, retained event tree, automatic layout owner, automatic mouse-to-focus policy, drag/drop framework, or independent pointer-protocol owner. Applications remain responsible for their event loop and command execution. Terminal remains authoritative for physical terminal state and reversible protocol leases.

## Targets

- .NET 8
- .NET 9
- .NET 10
- C# 13
- Windows x64/ARM64
- Linux x64/ARM64
- macOS x64/ARM64

## Quick start

```csharp
using Icod.DCurses;

await using CursesSession session = await CursesSession.OpenAsync();
CursesWindow screen = session.StandardScreen;

screen.Clear();
screen.Move(
    0,
    0
);
screen.Write(
    "Hello from Icod.DCurses",
    new CursesStyle(
        CursesColor.Default,
        CursesColor.Default,
        CursesTextAttributes.Bold
    )
);
await session.RefreshAsync();

CursesEvent terminalEvent = await session.ReadEventAsync();
```

A `CursesSession` restores the presentation and Terminal-owned state it acquires when disposed. Applications should consume terminal input and lifecycle activity through the curses/Terminal ownership model rather than adding a parallel byte reader.

## 1.5 advanced interaction control

Version 1.5 extends the 1.4 router with additional bounded mechanisms while preserving the existing root/unscoped behavior for applications that do not opt in.

Explicit scopes establish modal interaction boundaries without introducing widgets:

```csharp
using CursesInteractionRouter router = new( session.Screen );
using CursesInteractionScope popupScope = router.RegisterScope();
using CursesInteractionRegion popup = router.RegisterRegion(
    new CursesInteractionRegionOptions(
        new CursesRectangle( 3, 8, 8, 32 )
    ) {
        Scope = popupScope,
        IsFocusable = true
    }
);
using CursesInteractionScopeLease active = router.ActivateScope( popupScope );
```

While an explicit scope is active, hit testing and logical focus are restricted to its subtree. Nested activation is descendant-only and LIFO. Ending a scope activation restores saved logical focus when it is eligible again; otherwise ordinary deterministic focus repair applies.

Logical focus now supports deterministic spatial movement as well as the published forward/backward traversal:

```csharp
_ = router.MoveFocus( CursesFocusDirection.Right );
_ = router.MoveFocus( CursesFocusDirection.Do²È="25Õ¹‘Ì€¤ì)Í¥‘•‰…È¹M•Ñ	½Õ¹‘Ì Í¥‘•‰…É	½Õ¹‘Ì€¤ì)‰½‘ä¹M•Ñ	½Õ¹‘Ì ‰½‘å	½Õ¹‘Ì€¤ì)‘¥…±½œ¹M•Ñ	½Õ¹‘Ì (€€€‰½‘å	½Õ¹‘Ì¹%¹Í•Ğ (€€€€€€€¹•ÜÕÉÍ•Í%¹Í•ÑÌ €È°€Ğ°€È°€Ğ€¤(€€€€¤(¤ì)€()ÕÉÍ•ÍA…¹•°¹I•Í¥é•€…¹M•Ñ	½Õ¹‘Í€ÁÉ•Í•ÉÙ”ÍÕÉÙ¥Ù¥¹œÕÁÁ•Èµ±•™ĞÉ•Ñ…¥¹•½¹Ñ•¹Ğ…¹Í•µ…¹Ñ¥Œµ•Ñ…‘…Ñ„°±…µÀÑ¡”É•Ñ…¥¹•ÕÉÍ½È…™Ñ•ÈÍ¡É¥¹¬°É•Á…¥Èİ¥‘Ñ µÑİ¼™½½ÑÁÉ¥¹ÑÌ…ĞÉ•Í¥é”‰½Õ¹‘…É¥•Ì°ÁÉ•Í•ÉÙ”Ù¥Í¥‰¥±¥Ñä½èµ½É‘•È½ÑÉ…¹ÍÁ…É•¹ä°…¹Ù…±¥‘…Ñ”Ñ¡”™¥¹…°ÍÉ••¸µÉ•±…Ñ¥Ù”É•Ñ…¹±”‰•™½É”µÕÑ…Ñ¥½¸¸()½È±¥Ù”É•Í¥é”°ÕÉÍ•Ì‘•±¥‰•É…Ñ•±ä‘½•Ì¹½ĞÉ•Ñ…¥¸±…å½ÕĞÉÕ±•Ì¸Q¡”…ÁÁ±¥…Ñ¥½¸É•½µÁÕÑ•Ì™É½´Ñ¡”Íå¹¡É½¹¥é•±½¥…°ÍÉ••¸è()Í¡…ÉÀ)ÕÉÍ•ÍI•Ñ…¹±”ÕÉÉ•¹Ğ€ôÍ•ÍÍ¥½¸¹MÉ••¸¹	½Õ¹‘Ìì(¼¼‘•É¥Ù”É•Ñ…¹±•Ì™É½´ÕÉÉ•¹Ğ(¼¼…ÁÁ±äÑ¡•´İ¥Ñ M•Ñ	½Õ¹‘Ì€¼I•Í¥é”)…İ…¥ĞÍ•ÍÍ¥½¸¹I•™É•Í¡Íå¹Œ ¤ì)€()Q¡”%½¹ÕÉÍ•Ì¹1…å½ÕĞ¹M…µÁ±•€ÁÉ½©•Ğ‘•µ½¹ÍÑÉ…Ñ•ÌÑ¡¥Ì•áÁ±¥¥Ğ±¥™•å±”µ‘É¥Ù•¸É•½µÁÕÑ…Ñ¥½¸µ½‘•°İ¥Ñ ½É‘¥¹…Éäİ¥¹‘½İÌÁ±ÕÌ„É•Ñ…¥¹•Á…¹•°¸((ŒŒ€Ä¸ÈÉ•Ñ…¥¹•Á…¹•±Ì…¹±…å•ÉÌ()=É‘¥¹…ÉäÕÉÍ•Í]¥¹‘½İ€¥¹ÍÑ…¹•ÌÉ•µ…¥¸Í¡…É•±½¥…°Ù¥•İÌ¸ÕÉÍ•ÍA…¹•±€¥Ì¥¹Ñ•¹Ñ¥½¹…±±ä‘¥™™•É•¹Ğè¥Ğ½İ¹Ì…¸¥¹‘•Á•¹‘•¹ĞÉ•Ñ…¥¹•ÍÕÉ™…”…¹Á…ÉÑ¥¥Á…Ñ•Ì¥¸„‘•Ñ•Éµ¥¹¥ÍÑ¥ŒÍÉ••¸µ½İ¹•èµ½É‘•ÈÍÑ…¬¸()Í¡…ÉÀ)ÕÍ¥¹œÕÉÍ•ÍA…¹•°‘¥…±½œ€ôÍ•ÍÍ¥½¸¹MÉ••¸¹É•…Ñ•A…¹•° (€€€É½Üè€Ì°(€€€½±Õµ¸è€Ø°(€€€É½İÌè€à°(€€€½±Õµ¹Ìè€ÌØ(¤ì()‘¥…±½œ¹½¹Ñ•¹Ñ]¥¹‘½Ü¹]É¥Ñ” €‰I•Ñ…¥¹•‘¥…±½œ½¹Ñ•¹Ğˆ€¤ì)‘¥…±½œ¹5½Ù•Q½Q½À ¤ì)…İ…¥ĞÍ•ÍÍ¥½¸¹I•™É•Í¡Íå¹Œ ¤ì)€()A…¹•±Ì…É”½Á…ÅÕ”‰ä‘•™…Õ±Ğ¸Á…¹•°…¸¥¹ÍÑ•…µ…­”½É‘¥¹…Éä‰±…¹¬•±±ÌÑÉ…¹ÍÁ…É•¹Ğè()Í¡…ÉÀ)ÕÍ¥¹œÕÉÍ•ÍA…¹•°½Ù•É±…ä€ôÍ•ÍÍ¥½¸¹MÉ••¸¹É•…Ñ•A…¹•° (€€€É½Üè€È°(€€€½±Õµ¸è€Ğ°(€€€É½İÌè€Ì°(€€€½±Õµ¹Ìè€ÈÀ(¤ì)½Ù•É±…ä¹QÉ…¹ÍÁ…É•¹ä€ôÕÉÍ•ÍA…¹•±QÉ…¹ÍÁ…É•¹ä¹	±…¹­•±±ÍQÉ…¹ÍÁ…É•¹Ğì)½Ù•É±…ä¹½¹Ñ•¹Ñ]¥¹‘½Ü¹]É¥Ñ” €‰½Ù•É±…äˆ€¤ì)€()Q¡”ÁÕ‰±¥Í¡•€Ä¸ÈÁ…¹•°½¹ÑÉ…Ğ¥¹±Õ‘•Ì¥¹‘•Á•¹‘•¹ĞÉ•Ñ…¥¹•½¹Ñ•¹Ğ°Í¡½Ü½¡¥‘”İ¥Ñ É•µ•µ‰•É•èµ½É‘•È°µ½Ù•µ•¹Ğ…¹É•±…Ñ¥Ù”½É‘•É¥¹œ°±¥ÁÁ¥¹œ°½Á…ÅÕ”½‰±…¹¬µÑÉ…¹ÍÁ…É•¹Ğ½µÁ½Í¥Ñ¥½¸°U¹¥½‘”İ¥‘Ñ µÑİ¼…¹Í•µ…¹Ñ¥Œµµ•Ñ…‘…Ñ„½¡•É•¹”°‘…µ…”µ‰½Õ¹‘•É•½µÁ½Í¥Ñ¥½¸°±¥Ù”Í•ÍÍ¥½¸É•™É•Í ½±¥™•å±”¥¹Ñ•É…Ñ¥½¸°…¹‘•Ñ•Éµ¥¹¥ÍÑ¥Œ½¹”µİ…ä¥ÍÁ½Í” ¥€É•µ½Ù…°¸()¥ÍÁ½Í…°É•µ½Ù•Ì„ÑÉ…¹Í¥•¹ĞÁ…¹•°™É½´¥ÑÌ½İ¹¥¹œÍÉ••¸Í¼É•Á•…Ñ•‘±äµÉ•…Ñ•Á½ÁÕÁÌ½‘¥…±½Ì…É”¹½ĞÉ•Ñ…¥¹•™½ÈÑ¡”ÍÉ••¸±¥™•Ñ¥µ”¸‘¥ÍÁ½Í•Á…¹•°…¹¹½Ğ‰”É•…ÑÑ…¡•½Èµ…¹¥ÁÕ±…Ñ•¸()Y•ÉÍ¥½¸€Ä¸Ì•áÑ•¹‘ÌÑ¡¥ÌÁÕ‰±¥Í¡•µ½‘•°İ¥Ñ É•Ñ…¥¹•É•Í¥é¥¹œì¥Ğ‘½•Ì¹½ĞÉ•Á±…”Á…¹•°½İ¹•ÉÍ¡¥À½È½µÁ½Í¥Ñ¥½¸Í•µ…¹Ñ¥Ì¸((ŒŒ€Ä¸ÄÍ•µ…¹Ñ¥Œµ•Ñ…‘…Ñ„…¹¡åÁ•É±¥¹­Ì()Y•ÉÍ¥½¸€Ä¸Ä…‘‘•Í•µ…¹Ñ¥Œµ•…¹¥¹œ…ÑÑ…¡•Ñ¼É•Ñ…¥¹•½¹Ñ•¹Ğ°‰•¥¹¹¥¹œİ¥Ñ ¡åÁ•É±¥¹­Ì°İ¡¥±”­••Á¥¹œÙ¥ÍÕ…°É•¹‘¥Ñ¥½¸¥¸ÕÉÍ•ÍMÑå±•€¸()Í¡…ÉÀ)ÕÉÍ•Í•±±5•Ñ…‘…Ñ„µ•Ñ…‘…Ñ„€ô¹•Ü (€€€¹•ÜÕÉÍ•Í!åÁ•É±¥¹¬ (€€€€€€€€‰¡ÑÑÁÌè¼½•á…µÁ±”¹Ñ•ÍĞ½‘½Ìˆ°(€€€€€€€€‰‘½Ìˆ(€€€€¤(¤ì()ÍÉ••¸¹]É¥Ñ•]¥Ñ¡5•Ñ…‘…Ñ„ (€€€€‰‘½Õµ•¹Ñ…Ñ¥½¸ˆ°(€€€µ•Ñ…‘…Ñ„(¤ì)€()5•Ñ…‘…Ñ„¥ÌÉ•Ñ…¥¹•¥¹‘•Á•¹‘•¹Ñ±ä½˜Ù¥Í¥‰±”±åÁ ½ÍÑå±”•ÅÕ…±¥Ñä°™½±±½İÌ½¹Ñ•¹ĞÑ¡É½Õ ÍÕÁÁ½ÉÑ••‘¥Ñ¥¹œ½½µÁ½Í¥Ñ¥½¸½Á•É…Ñ¥½¹Ì°É•µ…¥¹Ì½¡•É•¹Ğ…É½ÍÌÑİ¼µ½±Õµ¸±•…‘•È½½¹Ñ¥¹Õ…Ñ¥½¸™½½ÑÁÉ¥¹ÑÌ°…¹¥Ì•µ¥ÑÑ•Á¡åÍ¥…±±äÑ¡É½Õ Q•Éµ¥¹…°µ½İ¹•Í•µ…¹Ñ¥Œ¡åÁ•É±¥¹¬½Á•É…Ñ¥½¹Ì¸ÕÉÍ•Ì‘½•Ì¹½Ğ½¹ÍÑÉÕĞ=M€à‘¥É•Ñ±ä¸((ŒŒA…‘Ì°U¹¥½‘”°…¹Í•µ…¹Ñ¥Œ‘É…İ¥¹œ()ÕÉÍ•ÍA…‘€¥Ì…¸½™˜µÍÉ••¸±½¥…°ÍÕÉ™…”Ñ¡…ĞÉ•ÕÍ•Ì½É‘¥¹…ÉäÕÉÍ•Í]¥¹‘½İ€•‘¥Ñ¥¹œÍ•µ…¹Ñ¥Ì¸5Õ±Ñ¥Á±”Ù¥•İÁ½ÉÑÌµ…ä½‰Í•ÉÙ”½¹”Á…¥¹‘•Á•¹‘•¹Ñ±ä¸()Q¡”‰Õ¥±Ğµ¥¸İ¥‘Ñ ÁÉ½Ù¥‘•È¥ÌÁ¥¹¹•Ñ¼U¹¥½‘”€ÄÜ¸À¸À¸…ÍĞÍ¥…¸µ‰¥Õ½ÕÌ¡…É…Ñ•ÉÌ…É”¹…ÉÉ½Ü‰ä‘•™…Õ±Ğ…¹…¸‰”µ…‘”İ¥‘”•áÁ±¥¥Ñ±äİ¥Ñ U¹¥½‘•ÕÉÍ•ÍQ•áÑ]¥‘Ñ¡AÉ½Ù¥‘•È¹]¥‘•µ‰¥Õ½ÕÍ%¹ÍÑ…¹•€¸()ÕÉÍ•ÍQ•áĞ¹5•…ÍÕÉ•½±Õµ¹Í€°QÉÕ¹…Ñ•Q½½±Õµ¹Í€°…¹M±¥•	å½±Õµ¹Í€½Á•É…Ñ”½¸½µÁ±•Ñ”Ñ•Éµ¥¹…°Ñ•áĞ•±•µ•¹ÑÌ…¹¹•Ù•ÈÉ•ÑÕÉ¸¡…±˜½˜„Ñİ¼µ½±Õµ¸•±•µ•¹Ğ¸M•µ…¹Ñ¥Œ±¥¹”•±±ÌÉ•µ…¥¸‘¥ÍÑ¥¹Ğ™É½´½É‘¥¹…ÉäU¹¥½‘”‰½àµ‘É…İ¥¹œÑ•áĞ¸((ŒŒ½¹ÕÉÉ•¹ä…¹±¥™•å±”()Q¡”±¥‰É…Éä‘•±¥‰•É…Ñ•±äÕÍ•Ì„¹…ÉÉ½Ü½İ¹•ÉÍ¡¥Àµ½‘•°É…Ñ¡•ÈÑ¡…¸Á•ÉÙ…Í¥Ù”Á•Èµ•±°±½­¥¹œè((´±½¥…°ÍÉ••¹Ì°İ¥¹‘½İÌ°Á…‘Ì°Ù¥•İÁ½ÉÑÌ°Á…¹•±Ì°…¹¥¹Ñ•É…Ñ¥½¸É½ÕÑ•ÉÌ…É”Í¥¹±”µİÉ¥Ñ•ÈÕ¹±•ÍÌ‘½Õµ•¹Ñ•½Ñ¡•Éİ¥Í”ì(´½¹”Q•Éµ¥¹…°µ½İ¹••Ù•¹Ğİ…¥Ğµ…ä½•á¥ÍĞİ¥Ñ Í•É¥…±¥é•É•™É•Í ½½ÕÑÁÕĞİ½É¬ì(´…±±•È…¹•±±…Ñ¥½¸‘½•Ì¹½Ğ‘¥Í…ÉQ•Éµ¥¹…°‘•½‘•ÈÍÑ…Ñ”ì(´‘¥ÍÁ½Í…°Õ¹‰±½­ÌÁ•¹‘¥¹œÕÉÍ•Ìİ…¥ÑÌİ¡¥±”ÁÉ•Í•ÉÙ¥¹œ…ÕÑ¡½É¥Ñ…Ñ¥Ù”É•ÍÑ½É…Ñ¥½¸ì(´½ÕÑÁÕĞÕ¹•ÉÑ…¥¹Ñä¥¹Ù…±¥‘…Ñ•ÌÉ•Ñ…¥¹•Á¡åÍ¥…°­¹½İ±•‘”Í¼„±…Ñ•ÈÉ•™É•Í …¸É•Á…¥¹ĞÍ…™•±äì(´ÍÕÍÁ•¹½É•ÍÕµ”¥¹Ù…±¥‘…Ñ•ÌÁ¡åÍ¥…°­¹½İ±•‘”‰ÕĞÉ•Ñ…¥¹Ì±½¥…°Á…¹•°…¹¥¹Ñ•É…Ñ¥½¸É•¥ÍÑÉ…Ñ¥½¸ÍÑ…Ñ”ì(´±¥™•å±”É•Í¥é”Íå¹¡É½¹¥é•ÌÑ¡”±½¥…°ÍÉ••¸°İ¡¥±”…ÁÁ±¥…Ñ¥½¸±…å½ÕĞÉ•½µÁÕÑ…Ñ¥½¸É•µ…¥¹Ì•áÁ±¥¥Ğì(´¥¹Ñ•É…Ñ¥½¸É½ÕÑ¥¹œ¹•Ù•ÈÉ•…Ñ•Ì„½µÁ•Ñ¥¹œ¥¹ÁÕĞÉ•…‘•È½ÈÑ•Éµ¥¹…°µ½ÕÑÁÕĞÁ…Ñ ¸((ŒŒY…±¥‘…Ñ¥½¸…¹Á…­…¥¹œ()1½…°İÉ…ÁÁ•ÉÌÕÍ”•‰Õœ½¹™¥ÕÉ…Ñ¥½¸¸AÕ±°É•ÅÕ•ÍÑÌÕÍ”MÑ…¥¹œİ¥Ñ İ…É¹¥¹Ìµ…Ìµ•ÉÉ½ÉÌ¸AÕÍ¡•ÌÑ¼µ…¥¹€…¹É•±•…Í”Ñ…ÌÕÍ”I•±•…Í”¸()IÕ¹Ñ¥µ”Ù…±¥‘…Ñ¥½¸½Ù•ÉÌ]¥¹‘½İÌ½1¥¹Õà½µ…=LàØĞ…¹I4ØĞìÑ¡”±¥‰É…Éä½Ñ•ÍĞµ…ÑÉ¥à½Ù•ÉÌ¹•Ğà¸Á€°¹•Ğä¸Á€°…¹¹•ĞÄÀ¸Á€¸()A…­…”Ù…±¥‘…Ñ¥½¸Ù•É¥™¥•Ì€¹¹ÕÁ­€½€¹Í¹ÕÁ­€°Á…­…”½…ÍÍ•µ‰±ä¥‘•¹Ñ¥Ñä°‘•Á•¹‘•¹äÉ½ÕÁÌ‘•É¥Ù•™É½´ÁÉ½©•Ğ‘•±…É…Ñ¥½¹Ì°I5½±¥•¹Í”½¥½¸½É•Á½Í¥Ñ½Éäµ•Ñ…‘…Ñ„°a50‘½Õµ•¹Ñ…Ñ¥½¸°Á½ÉÑ…‰±”Íåµ‰½±Ì°…¹„™É•Í 9Õ•Ğµ½¹±ä½¹ÍÕµ•È¸Q¡”Á…­…”½¹ÍÕµ•È½µÁ¥±•Ì…¹•á•ÕÑ•ÌÑ¡”½µÁ±•Ñ”…‘‘¥Ñ¥Ù”€Ä¸Ô¥¹Ñ•É…Ñ¥½¸ÍÕÉ™…”‘¥É•Ñ±ä™É½´Ñ¡”Á…­•…ÉÑ¥™…Ğ½¸¹•Ğà½¹•Ğä½¹•ĞÄÀ°¥¹±Õ‘¥¹œÍ½Á•Ì°ÍÁ…Ñ¥…°™½ÕÌ°Í½Á•½µµ…¹‘Ì°•áÁ±¥¥Ğ…ÁÑÕÉ”±¥™•Ñ¥µ”°…¹ÁÕ‰±¥ŒÁ½¥¹Ñ•ÈÑ…É•Ğ½•ÍÑÕÉ”½É•ÍÕ±Ğ½¹ÑÉ…ÑÌ¸A…­…”Ù…±¥‘…Ñ¥½¸‘½•Ì¹½Ğ¥µÁ½Í”¡…Éµ½‘•Í¥‰±¥¹œ‘•Á•¹‘•¹äÙ•ÉÍ¥½¹Ì¸((ŒŒI•±•…Í”‘½Õµ•¹Ñ…Ñ¥½¸()ÕÉÉ•¹Ğ…ÕÑ¡½É¥Ñ¥•Ìè((´%½¹ÕÉÍ•Ìµ•Ù•±½Áµ•¹ĞµI½…‘µ…À¹µ‘€(´%½¹ÕÉÍ•Ì´Ä¸Ô¸Àµ•Ù•±½Áµ•¹ĞµI½…‘µ…À¹µ‘€(´‘½Ì½AÕ‰±¥ŒµA$µ¥¹•ÉÁÉ¥¹Ğ´Ä¸Ô¹©Í½¹€(´‘½Ì½PÄÔÀ´Ä¸Ô¸ÀµÉ¡¥Ñ•ÑÕÉ”µA$µI•É•Ğµ…¹µ½¹ÑÉ…ĞµÉ••é”¹µ‘€(´‘½Ì½PÄÔÄµ	½Õ¹‘•µ%¹Ñ•É…Ñ¥½¸µM½Á•Ì¹µ‘€(´‘½Ì½PÄÔÈµáÁ±¥¥ĞµA½¥¹Ñ•Èµ…ÁÑÕÉ”¹µ‘€(´‘½Ì½PÄÔÌµ•Ñ•Éµ¥¹¥ÍÑ¥ŒµMÁ…Ñ¥…°µ½ÕÌ¹µ‘€(´‘½Ì½PÄÔĞµ•Ñ•Éµ¥¹¥ÍÑ¥ŒµA½¥¹Ñ•Èµ•ÍÑÕÉ”µ9½Éµ…±¥é…Ñ¥½¸¹µ‘€(´‘½Ì½PÄÔÔµM½Á•µ½µµ…¹µ	¥¹‘¥¹Ìµ…¹µAÉ••‘•¹”¹µ‘€(´‘½Ì½PÄÔØµ½¡•É•¹”µ…¹µ‘Ù•ÉÍ…É¥…°µ!…É‘•¹¥¹œ¹µ‘€(´‘½Ì½PÄÔÜµÁÁ±¥…Ñ¥½¸µ•ÁÑ…¹”µ…¹µA…­…”µ½¹ÍÕµ•È¹µ‘€(´‘½Ì½PÄÔàµ‘Ù…¹•µ%¹Ñ•É…Ñ¥½¸µI•É•Ğµ…¹µEÕ…±¥™¥…Ñ¥½¸µ…Ñ”¹µ‘€(´‘½Ì½PÄÔäµIµ…¹µMÑ…‰±”´Ä¸Ô¸Àµ±½ÍÕÉ”¹µ‘€()AÕ‰±¥Í¡•€Ä¸Ğ½µÁ…Ñ¥‰¥±¥Ñä½É•±•…Í”…ÕÑ¡½É¥Ñ¥•ÌÉ•µ…¥¸¡¥ÍÑ½É¥…°•Ù¥‘•¹”…¹…É”¹½ĞÉ•İÉ¥ÑÑ•¸Ñ¼Í¥µÕ±…Ñ”ÕÉÉ•¹Ğ€Ä¸Ô‘•Ù•±½Áµ•¹ĞÍÑ…Ñ”¸()PÄÔà™¥¹…°•Ù¥‘•¹”¡•…€ÈÄá˜äÀÁ……˜ØàäÀÈå˜á˜Õ‘”ÈØäÀÙ˜àØÜÈÑÀÈå•‰€Á…ÍÍ•€ŒàäÌ€¼€ÌĞäÄÔÌäÀÌàÅ€¸I¡•…€ÈÌÄÄÍˆÄÌÁØÜÑ‘„ÌÄÕ‰‰ÌàÑ„ØÁ„Á”ÙˆĞİ‰……€Á…ÍÍ•€Œàää€¼€ÌĞääÌààĞÄÀá€°…±°Í•Ù•¸©½‰Ì¸MÑ…‰±”µÍ½ÕÉ”¡•……˜ÌÁ„ÙŒÉ‘˜àĞÉÜÙˆáˆå”åˆÜàÔÕ…•ˆÍ„ÄÙ”å™˜å€Á…ÍÍ•€ŒäÀÔ€¼€ÌĞääĞÜØÄÜÜİ€°…±°Í•Ù•¸©½‰Ì¸AÉ½‘ÕÑ¥½¸Í½ÕÉ”…¹Ñ¡”™É½é•¸ÁÕ‰±¥ŒA$É•µ…¥¹•Õ¹¡…¹•Ñ¡É½Õ I½ÍÑ…‰±”µÍ½ÕÉ”ÁÉ½µ½Ñ¥½¸¸((ŒŒÕÑ¡½ÉÌ()%¹ÍÁ¥É•‰ä½É¥¥¹…°İ½É¬™É½´	¥±°)½ä°…ÕÑ¡½È½˜Ñ¡”½É¥¥¹…°Ñ•Éµ…Á€ì5…Éä¹¸€¡‰½É¸5…É¬¤!½ÉÑ½¸°…ÕÑ¡½È½˜Ñ•Éµ¥¹™½€ìA…Ù•°ÕÉÑ¥Ì°…ÕÑ¡½È½˜ÁÕÉÍ•Í€ì…¹i•å	•¸µ!…±¥´°É¥ŒL¸I…åµ½¹°…¹Q¡½µ…Ì¥­•ä°İ¡½Í”İ½É¬‘•Ù•±½Á•…¹µ…¥¹Ñ…¥¹•±¥‰Ñ¥¹™½€…¹¹ÕÉÍ•Í€¸()5…¹…•€¹9P¥µÁ±•µ•¹Ñ…Ñ¥½¸‰äQ¥µ½Ñ¡ä(¸	ÉÕ”€ñÕ¹¥‰±…‰¡½Ñµ…¥°¹½´ø¸((ŒŒ½ÁåÉ¥¡Ğ()½ÁåÉ¥¡Ğ€¡Œ¤€ÈÀÈØQ¥µ½Ñ¡ä(¸	ÉÕ”((ŒŒ1¥•¹Í”()1¥•¹Í•Õ¹‘•ÈÑ¡”9T1•ÍÍ•È•¹•É…°AÕ‰±¥Œ1¥•¹Í”ØÌ¸À½È±…Ñ•È¸M•”1%9M€¸()Q¡”9Õ•ĞÁ…­…”‘•±…É•Ì±¥•¹Í”…•ÁÑ…¹”…ÌÉ•ÅÕ¥É•¸A…­…”±¥•¹ÑÌİ¡¥ ¡½¹½È9Õ•ĞÌÉ•ÅÕ¥É•1¥•¹Í••ÁÑ…¹•€µ•Ñ…‘…Ñ„µÕÍĞ½‰Ñ…¥¸…•ÁÑ…¹”½˜Ñ¡”±¥•¹Í”Ñ•ÉµÌ‰•™½É”¥¹ÍÑ…±±…Ñ¥½¸¸(