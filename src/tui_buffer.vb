' verbose-tui — Double-Buffered Cell Grid & Diffed 24-Bit ANSI Renderer
Include "tui_layout.vb"

Public Structure TuiBuffer
  Dim width, height, capCells As Integer
  Dim cells As Pointer      ' Pointer to array of TuiCell (back buffer)
  Dim prevCells As Pointer  ' Pointer to array of TuiCell (front buffer for diffing)
  Dim outBuf As Pointer     ' Scratch byte buffer for ANSI output
  Dim outCap, outLen As Integer
  Dim forceRedraw As Boolean
End Structure

Public Function TuiBufferCreate(ByRef a As Arena, maxW, maxH As Integer) As TuiBuffer
  Dim cap As Integer = maxW * maxH
  Dim cellBytes As Integer = cap * SizeOf(TuiCell)
  Dim outCap As Integer = cap * 32 + 4096
  Dim b As TuiBuffer With (
    width = maxW,
    height = maxH,
    capCells = cap,
    cells = ArenaAllocBytes(ByRef a, cellBytes),
    prevCells = ArenaAllocBytes(ByRef a, cellBytes),
    outBuf = ArenaAllocBytes(ByRef a, outCap),
    outCap = outCap,
    outLen = 0,
    forceRedraw = True
  )
  TuiBufferClear(ByRef b, TuiStyleDefault())
  Return b
End Function

Public Sub TuiBufferResize(ByRef b As TuiBuffer, w, h As Integer)
  If w < 1 Then
    w = 1
  End If
  If h < 1 Then
    h = 1
  End If
  If w * h > b.capCells Then
    h = b.capCells / w
  End If
  b.width = w
  b.height = h
  b.forceRedraw = True
End Sub

Public Function TuiBufferArea(ByRef b As TuiBuffer) As TuiRect
  Return TuiRectMake(0, 0, b.width, b.height)
End Function

Private Function CellPtr(base As Pointer, idx As Integer) As Pointer
  Return PtrAdd(base, idx * SizeOf(TuiCell))
End Function

Public Sub TuiBufferClear(ByRef b As TuiBuffer, style As TuiStyle)
  Dim blank As TuiCell With ( ch = 32, style = style )
  Dim total As Integer = b.width * b.height
  For i = 0 To total - 1 Do
    PtrWrite(CellPtr(b.cells, i), TuiCell, blank)
  End For
End Sub

Public Sub TuiSetCell(ByRef b As TuiBuffer, x, y, ch As Integer, style As TuiStyle)
  If x < 0 Or y < 0 Or x >= b.width Or y >= b.height Then
    Return
  End If
  Dim c As TuiCell With ( ch = ch, style = style )
  PtrWrite(CellPtr(b.cells, y * b.width + x), TuiCell, c)
End Sub

Public Function TuiGetCell(ByRef b As TuiBuffer, x, y As Integer) As TuiCell
  If x < 0 Or y < 0 Or x >= b.width Or y >= b.height Then
    Dim empty As TuiCell With ( ch = 32, style = TuiStyleDefault() )
    Return empty
  End If
  Return PtrRead(CellPtr(b.cells, y * b.width + x), TuiCell)
End Function

Public Sub TuiFillRect(ByRef b As TuiBuffer, r As TuiRect, ch As Integer, style As TuiStyle)
  For row = r.y To r.y + r.height - 1 Do
    For col = r.x To r.x + r.width - 1 Do
      TuiSetCell(ByRef b, col, row, ch, style)
    End For
  End For
End Sub

Public Sub TuiPutStr(ByRef b As TuiBuffer, x, y, maxW As Integer, text As String, style As TuiStyle)
  If IsNull(text) Or maxW <= 0 Then
    Return
  End If
  Dim i As Integer = 0
  While text(i) <> 0 And i < maxW Do
    Dim ch As Integer = text(i)
    If ch >= 32 Then
      TuiSetCell(ByRef b, x + i, y, ch, style)
    End If
    i = i + 1
  End While
End Sub

' --- ANSI Output Buffer Builders ---
Private Sub EmitByte(ByRef b As TuiBuffer, ch As Byte)
  If b.outLen < b.outCap - 1 Then
    b.outBuf(b.outLen) = ch
    b.outLen = b.outLen + 1
  End If
End Sub

Private Sub EmitStr(ByRef b As TuiBuffer, s As String)
  Dim i As Integer = 0
  While s(i) <> 0 Do
    EmitByte(ByRef b, s(i))
    i = i + 1
  End While
End Sub

Private Sub EmitInt(ByRef b As TuiBuffer, v As Integer)
  Dim tmp(16) As Byte
  Dim n As Integer = TuiFmtInt(v, AddressOf(tmp(0)))
  For i = 0 To n - 1 Do
    EmitByte(ByRef b, tmp(i))
  End For
End Sub

Private Sub EmitCodepoint(ByRef b As TuiBuffer, cp As Integer)
  Dim tmp(8) As Byte
  Dim n As Integer = TuiUtf8Encode(cp, AddressOf(tmp(0)))
  For i = 0 To n - 1 Do
    EmitByte(ByRef b, tmp(i))
  End For
End Sub

Private Sub EmitMove(ByRef b As TuiBuffer, x, y As Integer)
  EmitByte(ByRef b, 27)
  EmitByte(ByRef b, 91)
  EmitInt(ByRef b, y + 1)
  EmitByte(ByRef b, 59)
  EmitInt(ByRef b, x + 1)
  EmitByte(ByRef b, 72)
End Sub

Private Sub EmitStyle(ByRef b As TuiBuffer, st As TuiStyle)
  EmitByte(ByRef b, 27)
  EmitStr(ByRef b, "[0")
  If (st.attrs And TUI_ATTR_BOLD) <> 0 Then
    EmitStr(ByRef b, ";1")
  End If
  If (st.attrs And TUI_ATTR_DIM) <> 0 Then
    EmitStr(ByRef b, ";2")
  End If
  If (st.attrs And TUI_ATTR_UNDERLINE) <> 0 Then
    EmitStr(ByRef b, ";4")
  End If
  If (st.attrs And TUI_ATTR_REVERSE) <> 0 Then
    EmitStr(ByRef b, ";7")
  End If
  If st.fg.isSet Then
    EmitStr(ByRef b, ";38;2;")
    EmitInt(ByRef b, st.fg.r)
    EmitByte(ByRef b, 59)
    EmitInt(ByRef b, st.fg.g)
    EmitByte(ByRef b, 59)
    EmitInt(ByRef b, st.fg.b)
  End If
  If st.bg.isSet Then
    EmitStr(ByRef b, ";48;2;")
    EmitInt(ByRef b, st.bg.r)
    EmitByte(ByRef b, 59)
    EmitInt(ByRef b, st.bg.g)
    EmitByte(ByRef b, 59)
    EmitInt(ByRef b, st.bg.b)
  End If
  EmitByte(ByRef b, 109) ' 'm'
End Sub

' Compute minimal ANSI diff into b.outBuf (returns byte length emitted)
Public Function TuiBufferDiff(ByRef b As TuiBuffer) As Integer
  b.outLen = 0
  Dim curStyle As TuiStyle = TuiStyleDefault()
  Dim hasStyle As Boolean = False
  Dim lastX As Integer = -2
  Dim lastY As Integer = -2

  For y = 0 To b.height - 1 Do
    For x = 0 To b.width - 1 Do
      Dim idx As Integer = y * b.width + x
      Dim cur As TuiCell = PtrRead(CellPtr(b.cells, idx), TuiCell)
      Dim prev As TuiCell = PtrRead(CellPtr(b.prevCells, idx), TuiCell)
      If b.forceRedraw Or Not TuiCellEq(cur, prev) Then
        If y <> lastY Or x <> lastX + 1 Then
          EmitMove(ByRef b, x, y)
        End If
        If Not hasStyle Or Not TuiStyleEq(cur.style, curStyle) Then
          EmitStyle(ByRef b, cur.style)
          curStyle = cur.style
          hasStyle = True
        End If
        EmitCodepoint(ByRef b, cur.ch)
        PtrWrite(CellPtr(b.prevCells, idx), TuiCell, cur)
        lastX = x
        lastY = y
      End If
    End For
  End For

  If b.outLen > 0 Then
    EmitByte(ByRef b, 27)
    EmitStr(ByRef b, "[0m")
  End If
  b.outBuf(b.outLen) = 0
  b.forceRedraw = False
  Return b.outLen
End Function

' Compute diff and flush to stdout in a single POSIX write()
Public Sub TuiBufferFlush(ByRef b As TuiBuffer)
  Dim n As Integer = TuiBufferDiff(ByRef b)
  If n > 0 Then
    TuiTermWriteBytes(b.outBuf, n)
  End If
End Sub
