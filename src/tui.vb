' verbose-tui — Umbrella Entry Point
' Usage:
'   Include "tui.vb"
' With vpm:
'   vpm add verbose-tui thetrung/verbose-tui && vpm run
' Or directly:
'   verbose app.vb -I src csrc/tui_term.c

Include "tui_types.vb"
Include "tui_layout.vb"
Include "tui_buffer.vb"
Include "tui_widgets.vb"
