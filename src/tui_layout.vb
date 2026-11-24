' verbose-tui — Ratatui-Style Rect & Layout Engine
Include "tui_types.vb"

Public Function TuiRectMake(x, y, w, h As Integer) As TuiRect
  If w < 0 Then
    w = 0
  End If
  If h < 0 Then
    h = 0
  End If
  Dim r As TuiRect With ( x = x, y = y, width = w, height = h )
  Return r
End Function

Public Function TuiRectInner(r As TuiRect, padX, padY As Integer) As TuiRect
  Return TuiRectMake(r.x + padX, r.y + padY, r.width - padX * 2, r.height - padY * 2)
End Function

' Vertical split: top gets `topH` rows, bottom gets remainder
Public Sub TuiSplitVFixed(area As TuiRect, topH As Integer, ByRef topOut, botOut As TuiRect)
  If topH > area.height Then
    topH = area.height
  End If
  If topH < 0 Then
    topH = 0
  End If
  topOut = TuiRectMake(area.x, area.y, area.width, topH)
  botOut = TuiRectMake(area.x, area.y + topH, area.width, area.height - topH)
End Sub

' Vertical 3-way split: fixed header + flexible body + fixed footer
Public Sub TuiSplitV3(area As TuiRect, headerH, footerH As Integer, ByRef headOut, bodyOut, footOut As TuiRect)
  If headerH + footerH > area.height Then
    headerH = area.height / 2
    footerH = area.height - headerH
  End If
  Dim bodyH As Integer = area.height - headerH - footerH
  headOut = TuiRectMake(area.x, area.y, area.width, headerH)
  bodyOut = TuiRectMake(area.x, area.y + headerH, area.width, bodyH)
  footOut = TuiRectMake(area.x, area.y + headerH + bodyH, area.width, footerH)
End Sub

' Horizontal percentage split: left gets `leftPct`% (0..100), right gets remainder
Public Sub TuiSplitHPercent(area As TuiRect, leftPct As Integer, ByRef leftOut, rightOut As TuiRect)
  If leftPct < 0 Then
    leftPct = 0
  End If
  If leftPct > 100 Then
    leftPct = 100
  End If
  Dim lw As Integer = (area.width * leftPct) / 100
  leftOut = TuiRectMake(area.x, area.y, lw, area.height)
  rightOut = TuiRectMake(area.x + lw, area.y, area.width - lw, area.height)
End Sub

' Vertical percentage split: top gets `topPct`% (0..100), bottom gets remainder
Public Sub TuiSplitVPercent(area As TuiRect, topPct As Integer, ByRef topOut, botOut As TuiRect)
  If topPct < 0 Then
    topPct = 0
  End If
  If topPct > 100 Then
    topPct = 100
  End If
  Dim th As Integer = (area.height * topPct) / 100
  topOut = TuiRectMake(area.x, area.y, area.width, th)
  botOut = TuiRectMake(area.x, area.y + th, area.width, area.height - th)
End Sub

Public Function TuiCenterRect(area As TuiRect, w, h As Integer) As TuiRect
  If w > area.width Then
    w = area.width
  End If
  If h > area.height Then
    h = area.height
  End If
  Dim cx As Integer = area.x + (area.width - w) / 2
  Dim cy As Integer = area.y + (area.height - h) / 2
  Return TuiRectMake(cx, cy, w, h)
End Function
