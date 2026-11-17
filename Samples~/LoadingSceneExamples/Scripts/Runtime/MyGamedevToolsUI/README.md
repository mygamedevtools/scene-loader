# My Gamedev Tools UI kit

The My Gamedev Tools brand for **runtime** UI Toolkit screens in package samples: loading screens,
HUDs and menus. It ships inside samples only. Games never depend on it, and editor windows don't
use it; they keep Unity's own look.

## Adding it to a sample

1. In the design-system repo, run `npm run unity:export -- --for <package>`, e.g.
   `--for scene-loader`. This gives the kit GUIDs unique to that package, so samples from two
   packages that both ship the kit never collide in a user's project.
2. Import `dist/MyGamedevToolsUI-<package>.unitypackage` into your package's dev project.
3. Move the `MyGamedevToolsUI` folder inside the sample's assembly definition folder, e.g.
   `Samples/LoadingSceneExamples/Scripts/Runtime/`. The kit has no assembly definition of its own,
   so its scripts must compile into the sample's; otherwise two samples would both put the same
   classes in Assembly-CSharp.
4. Point the sample's `PanelSettings` → **Theme Style Sheet** at
   `MyGamedevToolsUI/Theme/MyGamedevToolsTheme.tss`.

To update, export again with the same `--for` and re-import; the GUIDs are the same every time, so
references survive.

## Using it

- Add `mgt-root` to a screen's root element for the brand font and text color.
- Dark is the default. Add `mgt-theme-light` to any container to switch it to light.
- UXML elements (`xmlns:mgt="MyGameDevTools.UI"`):
  - `<mgt:BrandMark />` draws the H1 mark; size it with `width`/`height`.
  - `<mgt:CornerStripes />` draws the corner stripes; it fills its parent and bleeds off the top
    and right edges.
  - `<mgt:StatusIcon status="Warning" />` draws a shape-coded status icon.

| Class | Use |
| --- | --- |
| `mgt-screen`, `mgt-panel`, `mgt-divider` | Full-screen container, card, separator |
| `mgt-title` (`--sm`, `--xl`), `mgt-text` (`--muted`, `--strong`), `mgt-label`, `mgt-value`, `mgt-code` | Type |
| `mgt-eyebrow` + three `mgt-eyebrow__bar` (2nd `--2`, 3rd `--3`) | Stripe bars before a label |
| `mgt-rule` + three children (2nd `mgt-rule__segment--2`, 3rd `--3`) | Tri-color rule |
| `mgt-button` (`--primary`, `--small`, `--danger`) | Pill buttons |
| `mgt-progress` (`--thin`) + `mgt-progress__fill` | Progress bar; set the fill's `width` in percent |
| `mgt-badge` (`--success`, `--info`, `--warning`, `--error`, `--solid`) | Tags such as STABLE |
| `mgt-dot` (status modifiers) | Status dot |
| `mgt-callout` (status modifiers) with `StatusIcon`, `__content`, `__title`, `__body` | Tinted message |
| `mgt-phases` + `mgt-phase` (`--done`, `--current`, `--completed`, `--failed`) | An operation's timeline |
| `mgt-list-item`, `__head` | Clickable rows |

USS can't change text case, so write labels, eyebrows and badges in uppercase.

`Styles/Tokens.uss` is generated from the design-system tokens; change it there, not here.

## Licenses

Copyright (c) 2026 MyGameDevTools. All rights reserved. Russo One, Rethink Sans and Red Hat Mono are
third-party fonts under the SIL Open Font License (`Fonts/*-OFL.txt`).
