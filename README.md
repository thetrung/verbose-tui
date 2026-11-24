# verbose-tui

A **Ratatui-inspired**, zero-GC, double-buffered Terminal UI (TUI) framework for **[VerBose-Lang](https://github.com/thetrung/verbose-lang)** (`v0.6.0+`) — zero `ncurses` dependency, backed by an `Arena`-allocated cell grid and a tiny POSIX `termios`/`ioctl`/`poll` backend.

## Features

- **Zero-Flicker Double-Buffered Diff Renderer (`src/tui_buffer.vb`)**: Tracks front/back `TuiCell` grids in a deterministic `Arena`, skips unchanged cells, avoids redundant ANSI SGR escape sequences, and flushes each frame in a single POSIX `write()`.
- **24-Bit TrueColor RGB & Styles (`src/tui_types.vb`)**: Full RGB foreground/background colors plus Bold, Dim, Underline, and Reverse attributes.
- **Ratatui-Style Layout Engine (`src/tui_layout.vb`)**: `TuiRectMake`, `TuiRectInner`, `TuiSplitVFixed`, `TuiSplitV3`, `TuiSplitHPercent`, `TuiSplitVPercent`, and `TuiCenterRect`.
- **Built-In Widgets (`src/tui_widgets.vb`)**:
  - `TuiDrawBlock` (Rounded `╭─╮│╰╯`, Double `╔═╗║╚╝`, Plain `┌─┐│└┘`, ASCII borders + titles)
  - `TuiDrawParagraph` (clipped multi-line text)
  - `TuiDrawGauge` (labeled progress bar)
  - `TuiDrawSparkline` (8-level Unicode `▁▂▃▄▅▆▇█` bar chart)
  - `TuiDrawList` (scrollable selectable list with `▸` indicator)
  - `TuiDrawTabs` & `TuiDrawStatusBar`
- **`vpm`-Ready Package (`verbose.toml`)**: Install into any project via `vpm add verbose-tui thetrung/verbose-tui`.

## Quick Start

```bash
# Run automated headless tests + dashboard snapshot
make test

# Run the interactive Ratatui-style dashboard in your terminal
make run-dashboard
```

### Using with `verbose-pkg` (`vpm`)

```bash
vpm init my-tui-app
cd my-tui-app
vpm add verbose-tui thetrung/verbose-tui
```

```vb
Include "tui.vb"

Public Function main() As Integer
  Dim arena As Arena = ArenaCreate(1048576)
  Dim buf As TuiBuffer = TuiBufferCreate(ByRef arena, 80, 24)
  TuiDrawBlock(ByRef buf, TuiBufferArea(ByRef buf), "Hello VerBose TUI", TUI_BORDER_ROUNDED, TuiStyleFg(TuiCyan()), TuiStyleFg(TuiYellow()))
  Dim bytes As Integer = TuiBufferDiff(ByRef buf)
  Printf("Rendered %d ANSI bytes\n", bytes)
  ArenaDestroy(ByRef arena)
  Return 0
End Function
```
