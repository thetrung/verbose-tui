' verbose-tui — Ratatui-Inspired Widgets (Block, Paragraph, Gauge, Sparkline, List, Tabs, StatusBar)
Include "tui_buffer.vb"

' --- Block (Bordered Panel with Title, returns inner Rect) ---
Public Function TuiDrawBlock(ByRef b As TuiBuffer, area As TuiRect, title As String, borderKind As Integer, borderStyle, titleStyle As TuiStyle) As TuiRect
  If area.width < 2 Or area.height < 2 Then
    Return TuiRectMake(area.x, area.y, 0, 0)
  End If

  Dim tl, tr, bl, br, hz, vt As Integer = 0
  Match borderKind With
    Case TUI_BORDER_ROUNDED Then
      tl = 0x256D ' ╭
      tr = 0x256E ' ╮
      bl = 0x2570 ' ╰
      br = 0x256F ' ╯
      hz = 0x2500 ' ─
      vt = 0x2502 ' │
    Case TUI_BORDER_DOUBLE Then
      tl = 0x2554 ' ╔
      tr = 0x2557 ' ╗
      bl = 0x255A ' ╚
      br = 0x255D ' ╝
      hz = 0x2550 ' ═
      vt = 0x2551 ' ║
    Case TUI_BORDER_ASCII Then
      tl = 43     ' +
      tr = 43
      bl = 43
      br = 43
      hz = 45     ' -
      vt = 124    ' |
    Case _ Then
      tl = 0x250C ' ┌
      tr = 0x2510 ' ┐
      bl = 0x2514 ' └
      br = 0x2518 ' ┘
      hz = 0x2500 ' ─
      vt = 0x2502 ' │
  End Match

  Dim x0 As Integer = area.x
  Dim y0 As Integer = area.y
  Dim x1 As Integer = area.x + area.width - 1
  Dim y1 As Integer = area.y + area.height - 1

  TuiSetCell(ByRef b, x0, y0, tl, borderStyle)
  TuiSetCell(ByRef b, x1, y0, tr, borderStyle)
  TuiSetCell(ByRef b, x0, y1, bl, borderStyle)
  TuiSetCell(ByRef b, x1, y1, br, borderStyle)

  For x = x0 + 1 To x1 - 1 Do
    TuiSetCell(ByRef b, x, y0, hz, borderStyle)
    TuiSetCell(ByRef b, x, y1, hz, borderStyle)
  End For
  For y = y0 + 1 To y1 - 1 Do
    TuiSetCell(ByRef b, x0, y, vt, borderStyle)
    TuiSetCell(ByRef b, x1, y, vt, borderStyle)
  End For

  If Not IsNull(title) And area.width > 4 Then
    TuiSetCell(ByRef b, x0 + 2, y0, 32, titleStyle)
    Dim maxTitle As Integer = area.width - 5
    TuiPutStr(ByRef b, x0 + 3, y0, maxTitle, title, titleStyle)
    Dim tlen As Integer = 0
    While title(tlen) <> 0 And tlen < maxTitle Do
      tlen = tlen + 1
    End While
    TuiSetCell(ByRef b, x0 + 3 + tlen, y0, 32, titleStyle)
  End If

  Return TuiRectInner(area, 1, 1)
End Function

' --- Paragraph (multi-line text split on '\n', clipped to area) ---
Public Sub TuiDrawParagraph(ByRef b As TuiBuffer, area As TuiRect, text As String, style As TuiStyle)
  If IsNull(text) Or area.width <= 0 Or area.height <= 0 Then
    Return
  End If
  Dim row As Integer = 0
  Dim col As Integer = 0
  Dim i As Integer = 0
  While text(i) <> 0 And row < area.height Do
    Dim ch As Integer = text(i)
    If ch = 10 Then
      row = row + 1
      col = 0
    Else
      If col < area.width And ch >= 32 Then
        TuiSetCell(ByRef b, area.x + col, area.y + row, ch, style)
        col = col + 1
      End If
    End If
    i = i + 1
  End While
End Sub

' --- Gauge (progress bar 0..100 with optional centered label) ---
Public Sub TuiDrawGauge(ByRef b As TuiBuffer, area As TuiRect, percent As Integer, label As String, fillStyle, emptyStyle As TuiStyle)
  If area.width <= 0 Or area.height <= 0 Then
    Return
  End If
  If percent < 0 Then
    percent = 0
  End If
  If percent > 100 Then
    percent = 100
  End If
  Dim filledCols As Integer = (area.width * percent) / 100
  For row = 0 To area.height - 1 Do
    For col = 0 To area.width - 1 Do
      If col < filledCols Then
        TuiSetCell(ByRef b, area.x + col, area.y + row, 0x2588, fillStyle) ' █
      Else
        TuiSetCell(ByRef b, area.x + col, area.y + row, 0x2591, emptyStyle) ' ░
      End If
    End For
  End For

  If Not IsNull(label) Then
    Dim lLen As Integer = 0
    While label(lLen) <> 0 Do
      lLen = lLen + 1
    End While
    Dim lx As Integer = area.x + (area.width - lLen) / 2
    If lx < area.x Then
      lx = area.x
    End If
    Dim ly As Integer = area.y + area.height / 2
    Dim lblStyle As TuiStyle = TuiStyleFull(TuiWhite(), TuiDarkBg(), TUI_ATTR_BOLD)
    TuiPutStr(ByRef b, lx, ly, area.width, label, lblStyle)
  End If
End Sub

' --- Sparkline (8-level Unicode bar chart ▁▂▃▄▅▆▇█) ---
Public Sub TuiDrawSparkline(ByRef b As TuiBuffer, area As TuiRect, values() As Integer, count, maxVal As Integer, style As TuiStyle)
  If area.width <= 0 Or area.height <= 0 Or count <= 0 Or maxVal <= 0 Then
    Return
  End If
  Dim n As Integer = count
  If n > area.width Then
    n = area.width
  End If
  Dim baseY As Integer = area.y + area.height - 1
  For i = 0 To n - 1 Do
    Dim v As Integer = values(i)
    If v < 0 Then
      v = 0
    End If
    If v > maxVal Then
      v = maxVal
    End If
    Dim lvl As Integer = (v * 7) / maxVal
    Dim glyph As Integer = 0x2581 + lvl ' ▁..█
    TuiSetCell(ByRef b, area.x + i, baseY, glyph, style)
  End For
End Sub

' --- Selectable List ---
Public Sub TuiDrawList(ByRef b As TuiBuffer, area As TuiRect, items() As String, count, selected As Integer, normalStyle, selStyle As TuiStyle)
  If area.width <= 2 Or area.height <= 0 Then
    Return
  End If
  Dim scroll As Integer = 0
  If selected >= area.height Then
    scroll = selected - area.height + 1
  End If
  For row = 0 To area.height - 1 Do
    Dim idx As Integer = scroll + row
    If idx < count Then
      Dim st As TuiStyle = normalStyle
      Dim marker As Integer = 32
      If idx = selected Then
        st = selStyle
        marker = 0x25B8 ' ▸
        TuiFillRect(ByRef b, TuiRectMake(area.x, area.y + row, area.width, 1), 32, selStyle)
      End If
      TuiSetCell(ByRef b, area.x, area.y + row, marker, st)
      TuiPutStr(ByRef b, area.x + 2, area.y + row, area.width - 2, items(idx), st)
    End If
  End For
End Sub

' --- Tabs Bar ---
Public Sub TuiDrawTabs(ByRef b As TuiBuffer, area As TuiRect, titles() As String, count, active As Integer, normalStyle, activeStyle As TuiStyle)
  Dim cx As Integer = area.x + 1
  For i = 0 To count - 1 Do
    Dim st As TuiStyle = normalStyle
    If i = active Then
      st = activeStyle
    End If
    Dim title As String = titles(i)
    Dim tlen As Integer = 0
    While title(tlen) <> 0 Do
      tlen = tlen + 1
    End While
    If cx + tlen + 3 <= area.x + area.width Then
      TuiSetCell(ByRef b, cx, area.y, 32, st)
      TuiPutStr(ByRef b, cx + 1, area.y, tlen, title, st)
      TuiSetCell(ByRef b, cx + 1 + tlen, area.y, 32, st)
      cx = cx + tlen + 3
      If i < count - 1 And cx < area.x + area.width Then
        TuiSetCell(ByRef b, cx - 1, area.y, 0x2502, normalStyle) ' │
      End If
    End If
  End For
End Sub

' --- Status Bar ---
Public Sub TuiDrawStatusBar(ByRef b As TuiBuffer, area As TuiRect, leftText, rightText As String, style As TuiStyle)
  TuiFillRect(ByRef b, area, 32, style)
  TuiPutStr(ByRef b, area.x + 1, area.y, area.width - 2, leftText, style)
  If Not IsNull(rightText) Then
    Dim rlen As Integer = 0
    While rightText(rlen) <> 0 Do
      rlen = rlen + 1
    End While
    Dim rx As Integer = area.x + area.width - rlen - 1
    If rx > area.x Then
      TuiPutStr(ByRef b, rx, area.y, rlen, rightText, style)
    End If
  End If
End Sub
