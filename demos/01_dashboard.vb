' Demo 01: Ratatui-Style Interactive System Dashboard in VerBose-Lang
' Controls:
'   Tab / 1 / 2 / 3   - Switch tabs (Overview / Services / About)
'   Up / Down / j / k - Move selection in Services list
'   q / Esc / Ctrl-C  - Quit
' Supports headless `--snapshot` mode for CI/non-TTY testing!
Include "tui.vb"

Declare Function strcmp(a, b As String) As Integer

Public Function main(argc As Integer, argv() As Pointer) As Integer
  Dim snapshotMode As Boolean = False
  If argc >= 2 And strcmp(argv(1), "--snapshot") = 0 Then
    snapshotMode = True
  End If

  Dim arena As Arena = ArenaCreate(2097152)
  Dim buf As TuiBuffer = TuiBufferCreate(ByRef arena, 240, 80)

  Dim termW As Integer = 90
  Dim termH As Integer = 26
  If Not snapshotMode Then
    If TuiTermInit() = 0 Then
      snapshotMode = True
    Else
      TuiTermGetSize(ByRef termW, ByRef termH)
    End If
  End If
  TuiBufferResize(ByRef buf, termW, termH)

  Dim tabs(3) As String
  tabs(0) = "1: Overview"
  tabs(1) = "2: Services"
  tabs(2) = "3: About"

  Dim services(6) As String
  services(0) = "[RUNNING] verbose-compiler   (v0.6.0, 32 MiB Arena)"
  services(1) = "[RUNNING] verbose-raylib     (Raylib 5.x/6.x FFI)"
  services(2) = "[RUNNING] verbose-nvim-lsp   (Include-graph cache)"
  services(3) = "[RUNNING] verbose-pkg        (Serverless vpm)"
  services(4) = "[RUNNING] verbose-tui        (24-bit ANSI diff)"
  services(5) = "[IDLE]    arm-m-cross        (Thumbv7EM freestanding)"

  Dim spark(40) As Integer
  For i = 0 To 39 Do
    spark(i) = 20 + ((i * 17) Mod 75)
  End For

  Dim activeTab As Integer = 0
  Dim selectedSvc As Integer = 0
  Dim tick As Integer = 0
  Dim running As Boolean = True

  Dim bgStyle As TuiStyle = TuiStyleFull(TuiWhite(), TuiDarkBg(), 0)
  Dim borderStyle As TuiStyle = TuiStyleFull(TuiCyan(), TuiDarkBg(), 0)
  Dim titleStyle As TuiStyle = TuiStyleFull(TuiYellow(), TuiDarkBg(), TUI_ATTR_BOLD)
  Dim dimStyle As TuiStyle = TuiStyleFull(TuiGray(), TuiDarkBg(), 0)
  Dim selStyle As TuiStyle = TuiStyleFull(TuiDarkBg(), TuiCyan(), TUI_ATTR_BOLD)
  Dim tabActStyle As TuiStyle = TuiStyleFull(TuiDarkBg(), TuiYellow(), TUI_ATTR_BOLD)
  Dim statusStyle As TuiStyle = TuiStyleFull(TuiWhite(), TuiSurfaceBg(), 0)

  While running Do
    tick = tick + 1
    ' Animate sparkline & gauges
    For i = 0 To 38 Do
      spark(i) = spark(i + 1)
    End For
    spark(39) = 25 + ((tick * 13 + 7) Mod 70)

    Dim cpuPct As Integer = 32 + ((tick * 7) Mod 55)
    Dim memPct As Integer = 48 + ((tick * 3) Mod 24)
    Dim arenaPct As Integer = 18 + ((tick * 5) Mod 30)

    TuiBufferClear(ByRef buf, bgStyle)
    Dim root As TuiRect = TuiBufferArea(ByRef buf)
    Dim headR, bodyR, footR As TuiRect
    TuiSplitV3(root, 3, 1, ByRef headR, ByRef bodyR, ByRef footR)

    ' Header tabs
    Dim headIn As TuiRect = TuiDrawBlock(ByRef buf, headR, "VerBose-TUI Dashboard", TUI_BORDER_ROUNDED, borderStyle, titleStyle)
    TuiDrawTabs(ByRef buf, headIn, tabs, 3, activeTab, bgStyle, tabActStyle)

    ' Body by active tab
    Match activeTab With
      Case 0 Then
        Dim leftR, rightR As TuiRect
        TuiSplitHPercent(bodyR, 52, ByRef leftR, ByRef rightR)

        Dim topLeft, botLeft As TuiRect
        TuiSplitVPercent(leftR, 55, ByRef topLeft, ByRef botLeft)

        Dim gIn As TuiRect = TuiDrawBlock(ByRef buf, topLeft, "System Metrics", TUI_BORDER_ROUNDED, borderStyle, titleStyle)
        If gIn.height >= 6 Then
          TuiPutStr(ByRef buf, gIn.x + 1, gIn.y, gIn.width - 2, "CPU Load", bgStyle)
          TuiDrawGauge(ByRef buf, TuiRectMake(gIn.x + 1, gIn.y + 1, gIn.width - 2, 1), cpuPct, $"{cpuPct}%", TuiStyleFull(TuiCyan(), TuiDarkBg(), 0), dimStyle)
          TuiPutStr(ByRef buf, gIn.x + 1, gIn.y + 2, gIn.width - 2, "Memory Usage", bgStyle)
          TuiDrawGauge(ByRef buf, TuiRectMake(gIn.x + 1, gIn.y + 3, gIn.width - 2, 1), memPct, $"{memPct}%", TuiStyleFull(TuiGreen(), TuiDarkBg(), 0), dimStyle)
          TuiPutStr(ByRef buf, gIn.x + 1, gIn.y + 4, gIn.width - 2, "Arena Budget", bgStyle)
          TuiDrawGauge(ByRef buf, TuiRectMake(gIn.x + 1, gIn.y + 5, gIn.width - 2, 1), arenaPct, $"{arenaPct}%", TuiStyleFull(TuiPurple(), TuiDarkBg(), 0), dimStyle)
        End If

        Dim sIn As TuiRect = TuiDrawBlock(ByRef buf, botLeft, "Throughput Sparkline", TUI_BORDER_ROUNDED, borderStyle, titleStyle)
        TuiDrawSparkline(ByRef buf, TuiRectInner(sIn, 1, 0), spark, 40, 100, TuiStyleFull(TuiGreen(), TuiDarkBg(), 0))

        Dim rIn As TuiRect = TuiDrawBlock(ByRef buf, rightR, "Ecosystem Components", TUI_BORDER_ROUNDED, borderStyle, titleStyle)
        TuiDrawList(ByRef buf, rIn, services, 6, selectedSvc, bgStyle, selStyle)

      Case 1 Then
        Dim lIn As TuiRect = TuiDrawBlock(ByRef buf, bodyR, "Active Ecosystem Services (Up/Down to select)", TUI_BORDER_DOUBLE, borderStyle, titleStyle)
        TuiDrawList(ByRef buf, TuiRectInner(lIn, 1, 1), services, 6, selectedSvc, bgStyle, selStyle)

      Case _ Then
        Dim aIn As TuiRect = TuiDrawBlock(ByRef buf, bodyR, "About verbose-tui", TUI_BORDER_ROUNDED, borderStyle, titleStyle)
        Dim aboutTxt As String = "verbose-tui — Ratatui-inspired TUI framework for VerBose-Lang v0.6.0\n\n" &
          "Features:\n" &
          "  * Zero-GC Arena-backed double-buffered cell grid\n" &
          "  * Diffed 24-bit TrueColor ANSI rendering (zero flicker, no ncurses)\n" &
          "  * Unicode Rounded/Double/Plain/ASCII borders & 8-level Sparklines\n" &
          "  * Layout splits (fixed + percentage), Block, Gauge, List, Tabs, Paragraph\n" &
          "  * Packageable with verbose-pkg (vpm)\n"
        TuiDrawParagraph(ByRef buf, TuiRectInner(aIn, 2, 1), aboutTxt, bgStyle)
    End Match

    ' Status bar
    TuiDrawStatusBar(ByRef buf, footR, " [Tab] Switch Tab  [Up/Down] Select  [q] Quit ", $"Tick #{tick} | {termW}x{termH}", statusStyle)

    If snapshotMode Then
      Dim emitted As Integer = TuiBufferDiff(ByRef buf)
      Printf("[snapshot] Rendered %dx%d dashboard frame (%d ANSI bytes emitted)\n", termW, termH, emitted)
      running = False
    Else
      TuiBufferFlush(ByRef buf)
      Dim ev As TuiEvent = TuiPollEvent(120)
      Match ev.key With
        Case TUI_KEY_CTRL_C, TUI_KEY_ESC Then running = False
        Case TUI_KEY_TAB Then activeTab = (activeTab + 1) Mod 3
        Case TUI_KEY_UP Then
          If selectedSvc > 0 Then
            selectedSvc = selectedSvc - 1
          End If
        Case TUI_KEY_DOWN Then
          If selectedSvc < 5 Then
            selectedSvc = selectedSvc + 1
          End If
        Case TUI_KEY_RESIZE Then
          TuiTermGetSize(ByRef termW, ByRef termH)
          TuiBufferResize(ByRef buf, termW, termH)
        Case TUI_KEY_CHAR Then
          Match ev.ch With
            Case 113, 81 Then running = False ' q / Q
            Case 49 Then activeTab = 0        ' 1
            Case 50 Then activeTab = 1        ' 2
            Case 51 Then activeTab = 2        ' 3
            Case 106 Then                     ' j
              If selectedSvc < 5 Then
                selectedSvc = selectedSvc + 1
              End If
            Case 107 Then                     ' k
              If selectedSvc > 0 Then
                selectedSvc = selectedSvc - 1
              End If
            Case _ Then
          End Match
        Case _ Then
      End Match
    End If
  End While

  If Not snapshotMode Then
    TuiTermShutdown()
  End If
  ArenaDestroy(ByRef arena)
  Return 0
End Function
