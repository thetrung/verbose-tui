' verbose-tui — Automated Layout, Buffer Diff, Widget & Key-Decoder Verification Suite
Include "tui.vb"

Test "Layout splits (V3, HPercent, VPercent, CenterRect)"
  Dim screen As TuiRect = TuiRectMake(0, 0, 100, 30)
  Dim head, body, foot As TuiRect
  TuiSplitV3(screen, 3, 2, ByRef head, ByRef body, ByRef foot)
  Assert head.y = 0 And head.height = 3, "header rect"
  Assert body.y = 3 And body.height = 25, "body rect"
  Assert foot.y = 28 And foot.height = 2, "footer rect"

  Dim left, right As TuiRect
  TuiSplitHPercent(body, 40, ByRef left, ByRef right)
  Assert left.width = 40 And right.x = 40 And right.width = 60, "HPercent split"

  Dim modal As TuiRect = TuiCenterRect(screen, 40, 10)
  Assert modal.x = 30 And modal.y = 10 And modal.width = 40 And modal.height = 10, "CenterRect"
End Test

Test "Double-buffered cell grid diff skips unchanged frames"
  Dim a As Arena = ArenaCreate(524288)
  Dim buf As TuiBuffer = TuiBufferCreate(ByRef a, 40, 12)
  Dim st As TuiStyle = TuiStyleFull(TuiCyan(), TuiDarkBg(), TUI_ATTR_BOLD)

  TuiPutStr(ByRef buf, 2, 1, 20, "Hello TUI", st)
  Dim cell As TuiCell = TuiGetCell(ByRef buf, 2, 1)
  Assert cell.ch = 72, "Expected 'H' at (2,1)"

  Dim bytes1 As Integer = TuiBufferDiff(ByRef buf)
  Assert bytes1 > 0, "First frame must emit ANSI bytes"

  Dim bytes2 As Integer = TuiBufferDiff(ByRef buf)
  Assert bytes2 = 0, "Unchanged second frame must emit 0 bytes (diffed!)"

  TuiSetCell(ByRef buf, 0, 0, 88, st)
  Dim bytes3 As Integer = TuiBufferDiff(ByRef buf)
  Assert bytes3 > 0 And bytes3 < bytes1, "Single-cell delta should be tiny"

  ArenaDestroy(ByRef a)
End Test

Test "Widgets render Unicode borders, Gauge, Sparkline and List accurately"
  Dim a As Arena = ArenaCreate(524288)
  Dim buf As TuiBuffer = TuiBufferCreate(ByRef a, 50, 14)
  Dim st As TuiStyle = TuiStyleFg(TuiWhite())
  Dim sel As TuiStyle = TuiStyleFull(TuiDarkBg(), TuiCyan(), TUI_ATTR_BOLD)

  Dim inner As TuiRect = TuiDrawBlock(ByRef buf, TuiRectMake(0, 0, 30, 8), "Panel", TUI_BORDER_ROUNDED, st, st)
  Assert inner.x = 1 And inner.y = 1 And inner.width = 28 And inner.height = 6, "Block inner rect"
  Dim cTL As TuiCell = TuiGetCell(ByRef buf, 0, 0)
  Dim cBR As TuiCell = TuiGetCell(ByRef buf, 29, 7)
  Assert cTL.ch = 0x256D, "Top-left rounded corner"
  Assert cBR.ch = 0x256F, "Bottom-right rounded corner"

  TuiDrawGauge(ByRef buf, TuiRectMake(1, 1, 20, 1), 50, "", st, st)
  Dim cG1 As TuiCell = TuiGetCell(ByRef buf, 1, 1)
  Dim cG2 As TuiCell = TuiGetCell(ByRef buf, 20, 1)
  Assert cG1.ch = 0x2588, "Gauge filled cell"
  Assert cG2.ch = 0x2591, "Gauge empty cell"

  Dim vals(4) As Integer = (0, 50, 100, 25)
  TuiDrawSparkline(ByRef buf, TuiRectMake(1, 3, 10, 1), vals, 4, 100, st)
  Dim cS1 As TuiCell = TuiGetCell(ByRef buf, 1, 3)
  Dim cS2 As TuiCell = TuiGetCell(ByRef buf, 3, 3)
  Assert cS1.ch = 0x2581, "Sparkline min"
  Assert cS2.ch = 0x2588, "Sparkline max"

  Dim items(2) As String
  items(0) = "Alpha"
  items(1) = "Beta"
  TuiDrawList(ByRef buf, TuiRectMake(1, 5, 20, 2), items, 2, 1, st, sel)
  Dim cSel As TuiCell = TuiGetCell(ByRef buf, 1, 6)
  Assert cSel.ch = 0x25B8, "Selected item indicator"

  ArenaDestroy(ByRef a)
End Test

Test "ANSI escape sequence key decoder"
  Dim upSeq(3) As Byte = (27, 91, 65)
  Dim ch As Integer = 0
  Assert TuiDecodeKeyBytes(AddressOf(upSeq(0)), 3, ByRef ch) = TUI_KEY_UP, "ESC [ A -> Up"

  Dim ctrlC(1) As Byte = (3)
  Assert TuiDecodeKeyBytes(AddressOf(ctrlC(0)), 1, ByRef ch) = TUI_KEY_CTRL_C, "0x03 -> Ctrl-C"

  Dim letterQ(1) As Byte = (113)
  Assert TuiDecodeKeyBytes(AddressOf(letterQ(0)), 1, ByRef ch) = TUI_KEY_CHAR And ch = 113, "'q' -> TUI_KEY_CHAR"
End Test
