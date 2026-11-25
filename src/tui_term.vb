' verbose-tui — 100% Pure VerBose POSIX Terminal Backend (termios + ioctl + poll + UTF-8/ANSI)
' Zero C files required: binds directly to libc POSIX functions via Declare & ABI-accurate Structs.

Public Structure PosixTermios
  Dim c_iflag, c_oflag, c_cflag, c_lflag As Integer
  Dim c_line As Byte
  Dim c_cc(32) As Byte
  Dim pad0, pad1, pad2 As Byte
  Dim c_ispeed, c_ospeed As Integer
End Structure

Public Structure PosixWinsize
  Dim ws_row, ws_col, ws_xpixel, ws_ypixel As Short
End Structure

Public Structure PosixPollFd
  Dim fd As Integer
  Dim events, revents As Short
End Structure

Declare Function isatty(fd As Integer) As Integer
Declare Function tcgetattr(fd As Integer, ByRef t As PosixTermios) As Integer
Declare Function tcsetattr(fd, optActions As Integer, ByRef t As PosixTermios) As Integer
Declare Function ioctl(fd As Integer, req As Long, ...) As Integer
Declare Function poll(ByRef fds As PosixPollFd, nfds As Long, timeout As Integer) As Integer
Declare Function read(fd As Integer, buf As Pointer, count As Long) As Long
Declare Function write(fd As Integer, buf As Pointer, count As Long) As Long
Declare Function signal(sig As Integer, handler As Pointer) As Pointer
Declare Function atexit(fn As Pointer) As Integer
Declare Sub _exit(status As Integer)
Declare Function strlen(s As Pointer) As Long
Declare Function snprintf(buf As Pointer, sz As Long, fmt As Pointer, ...) As Integer

Private Static g_origTermios As PosixTermios
Private Static g_rawActive As Integer = 0
Private Volatile Static g_resized As Integer = 0

Private Sub TuiHandleSigWinch(sig As Integer)
  g_resized = 1
End Sub

Private Sub TuiWriteAll(buf As Pointer, length As Long)
  Dim off As Long = 0
  While off < length Do
    Dim n As Long = write(1, PtrAdd(buf, off), length - off)
    If n <= 0 Then
      Break
    End If
    off = off + n
  End While
End Sub

Public Sub TuiTermShutdown()
  If g_rawActive = 0 Then
    Return
  End If
  Dim restore As String = "\x1b[0m\x1b[?25h\x1b[?1049l"
  TuiWriteAll(restore, strlen(restore))
  tcsetattr(0, 2, ByRef g_origTermios)
  g_rawActive = 0
End Sub

Private Sub TuiHandleSigInt(sig As Integer)
  TuiTermShutdown()
  _exit(130)
End Sub

Public Function TuiTermInit() As Integer
  If (isatty(0) = 0) Or (isatty(1) = 0) Then
    Return 0
  End If
  If tcgetattr(0, ByRef g_origTermios) <> 0 Then
    Return 0
  End If
  Dim raw As PosixTermios = g_origTermios
  ' Linux termios flags: IFLAG_MASK=0x532, OPOST=0x1, CS8=0x30, LFLAG_MASK=0x800B, VMIN=6, VTIME=5
  raw.c_iflag = raw.c_iflag And (Not 0x532)
  raw.c_oflag = raw.c_oflag And (Not 0x1)
  raw.c_cflag = raw.c_cflag Or 0x30
  raw.c_lflag = raw.c_lflag And (Not 0x800B)
  raw.c_cc(6) = Byte(0)
  raw.c_cc(5) = Byte(0)

  If tcsetattr(0, 2, ByRef raw) <> 0 Then
    Return 0
  End If
  g_rawActive = 1
  atexit(AddressOf(TuiTermShutdown))
  signal(28, AddressOf(TuiHandleSigWinch))
  signal(2, AddressOf(TuiHandleSigInt))
  signal(15, AddressOf(TuiHandleSigInt))

  Dim enter As String = "\x1b[?1049h\x1b[?25l\x1b[2J\x1b[H"
  TuiWriteAll(enter, strlen(enter))
  Return 1
End Function

Public Sub TuiTermGetSize(ByRef outW, outH As Integer)
  Dim ws As PosixWinsize
  ' TIOCGWINSZ = 0x5413 on Linux
  If (ioctl(1, 0x5413, AddressOf(ws)) = 0) And (Integer(ws.ws_col) > 0) And (Integer(ws.ws_row) > 0) Then
    outW = Integer(ws.ws_col)
    outH = Integer(ws.ws_row)
  Else
    outW = 80
    outH = 24
  End If
End Sub

Public Function TuiTermWasResized() As Integer
  If g_resized <> 0 Then
    g_resized = 0
    Return 1
  End If
  Return 0
End Function

Public Sub TuiTermWriteBytes(data As Pointer, length As Integer)
  If (Not IsNull(data)) And (length > 0) Then
    TuiWriteAll(data, Long(length))
  End If
End Sub

Public Function TuiUtf8Encode(cp As Integer, dst As Pointer) As Integer
  If cp < 0x80 Then
    PtrWrite(dst, Byte, Byte(cp))
    Return 1
  End If
  If cp < 0x800 Then
    PtrWrite(dst, Byte, Byte(0xC0 Or (cp Shr 6)))
    PtrWrite(PtrAdd(dst, 1), Byte, Byte(0x80 Or (cp And 0x3F)))
    Return 2
  End If
  If cp < 0x10000 Then
    PtrWrite(dst, Byte, Byte(0xE0 Or (cp Shr 12)))
    PtrWrite(PtrAdd(dst, 1), Byte, Byte(0x80 Or ((cp Shr 6) And 0x3F)))
    PtrWrite(PtrAdd(dst, 2), Byte, Byte(0x80 Or (cp And 0x3F)))
    Return 3
  End If
  PtrWrite(dst, Byte, Byte(0xF0 Or (cp Shr 18)))
  PtrWrite(PtrAdd(dst, 1), Byte, Byte(0x80 Or ((cp Shr 12) And 0x3F)))
  PtrWrite(PtrAdd(dst, 2), Byte, Byte(0x80 Or ((cp Shr 6) And 0x3F)))
  PtrWrite(PtrAdd(dst, 3), Byte, Byte(0x80 Or (cp And 0x3F)))
  Return 4
End Function

Public Function TuiDecodeKeyBytes(buf As Pointer, length As Integer, ByRef outCh As Integer) As Integer
  outCh = 0
  If IsNull(buf) Or (length <= 0) Then
    Return TUI_KEY_NONE
  End If
  Dim c As Integer = Integer(PtrRead(buf, Byte)) And 255
  If c = 3 Then
    Return TUI_KEY_CTRL_C
  End If
  If (c = 13) Or (c = 10) Then
    Return TUI_KEY_ENTER
  End If
  If c = 9 Then
    Return TUI_KEY_TAB
  End If
  If (c = 127) Or (c = 8) Then
    Return TUI_KEY_BACKSPACE
  End If
  If c = 27 Then
    If length >= 3 Then
      Dim b1 As Integer = Integer(PtrRead(PtrAdd(buf, 1), Byte)) And 255
      Dim b2 As Integer = Integer(PtrRead(PtrAdd(buf, 2), Byte)) And 255
      If b1 = 91 Then
        Match b2 With
          Case 65 Then Return TUI_KEY_UP
          Case 66 Then Return TUI_KEY_DOWN
          Case 67 Then Return TUI_KEY_RIGHT
          Case 68 Then Return TUI_KEY_LEFT
          Case 72 Then Return TUI_KEY_HOME
          Case 70 Then Return TUI_KEY_END
          Case 51 Then
            If (length >= 4) And ((Integer(PtrRead(PtrAdd(buf, 3), Byte)) And 255) = 126) Then
              Return TUI_KEY_DELETE
            End If
          Case 53 Then
            If (length >= 4) And ((Integer(PtrRead(PtrAdd(buf, 3), Byte)) And 255) = 126) Then
              Return TUI_KEY_PAGE_UP
            End If
          Case 54 Then
            If (length >= 4) And ((Integer(PtrRead(PtrAdd(buf, 3), Byte)) And 255) = 126) Then
              Return TUI_KEY_PAGE_DOWN
            End If
        End Match
      End If
    End If
    Return TUI_KEY_ESC
  End If
  outCh = c
  Return TUI_KEY_CHAR
End Function

Public Function TuiTermPollKey(timeoutMs As Integer, ByRef outCh As Integer) As Integer
  outCh = 0
  If TuiTermWasResized() <> 0 Then
    Return TUI_KEY_RESIZE
  End If
  Dim pfd As PosixPollFd With ( fd = 0, events = Short(1), revents = Short(0) )
  Dim rc As Integer = poll(ByRef pfd, 1, timeoutMs)
  If rc <= 0 Then
    If TuiTermWasResized() <> 0 Then
      Return TUI_KEY_RESIZE
    End If
    Return TUI_KEY_NONE
  End If
  Dim buf(16) As Byte
  Dim n As Long = read(0, buf, 16)
  If n <= 0 Then
    Return TUI_KEY_NONE
  End If
  Return TuiDecodeKeyBytes(buf, Integer(n), ByRef outCh)
End Function

Public Function TuiFmtInt(v As Integer, dst As Pointer) As Integer
  Return snprintf(dst, 16, "%d", v)
End Function
