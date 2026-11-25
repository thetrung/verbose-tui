' verbose-tui — Core Types, Colors, Styles, Cell, Events & C Backend FFI

Public Enum TuiKey
  TUI_KEY_NONE = 0
  TUI_KEY_CHAR = 1
  TUI_KEY_ENTER = 2
  TUI_KEY_ESC = 3
  TUI_KEY_BACKSPACE = 4
  TUI_KEY_TAB = 5
  TUI_KEY_UP = 6
  TUI_KEY_DOWN = 7
  TUI_KEY_LEFT = 8
  TUI_KEY_RIGHT = 9
  TUI_KEY_HOME = 10
  TUI_KEY_END = 11
  TUI_KEY_PAGE_UP = 12
  TUI_KEY_PAGE_DOWN = 13
  TUI_KEY_DELETE = 14
  TUI_KEY_CTRL_C = 15
  TUI_KEY_RESIZE = 16
End Enum

Public Enum TuiBorderStyle
  TUI_BORDER_PLAIN = 0
  TUI_BORDER_ROUNDED = 1
  TUI_BORDER_DOUBLE = 2
  TUI_BORDER_ASCII = 3
End Enum

Public Enum TuiAttr
  TUI_ATTR_NONE = 0
  TUI_ATTR_BOLD = 1
  TUI_ATTR_DIM = 2
  TUI_ATTR_UNDERLINE = 4
  TUI_ATTR_REVERSE = 8
End Enum

Public Structure TuiRect
  Dim x, y, width, height As Integer
End Structure

Public Structure TuiColor
  Dim isSet As Boolean
  Dim r, g, b As Byte
End Structure

Public Structure TuiStyle
  Dim fg, bg As TuiColor
  Dim attrs As Integer
End Structure

Public Structure TuiCell
  Dim ch As Integer
  Dim style As TuiStyle
End Structure

Public Structure TuiEvent
  Dim key, ch As Integer
End Structure

' --- Pure VerBose POSIX Terminal Backend (src/tui_term.vb) ---
Include "tui_term.vb"

' --- Color & Style Constructors ---
Public Function TuiColorDefault() As TuiColor
  Dim c As TuiColor With ( isSet = False, r = 0, g = 0, b = 0 )
  Return c
End Function

Public Function TuiRgb(r, g, b As Byte) As TuiColor
  Dim c As TuiColor With ( isSet = True, r = r, g = g, b = b )
  Return c
End Function

Public Function TuiStyleDefault() As TuiStyle
  Dim s As TuiStyle With ( fg = TuiColorDefault(), bg = TuiColorDefault(), attrs = 0 )
  Return s
End Function

Public Function TuiStyleFg(fg As TuiColor) As TuiStyle
  Dim s As TuiStyle With ( fg = fg, bg = TuiColorDefault(), attrs = 0 )
  Return s
End Function

Public Function TuiStyleFull(fg, bg As TuiColor, attrs As Integer) As TuiStyle
  Dim s As TuiStyle With ( fg = fg, bg = bg, attrs = attrs )
  Return s
End Function

Public Function TuiColorEq(a, b As TuiColor) As Boolean
  If a.isSet <> b.isSet Then
    Return False
  End If
  If Not a.isSet Then
    Return True
  End If
  Return a.r = b.r And a.g = b.g And a.b = b.b
End Function

Public Function TuiStyleEq(a, b As TuiStyle) As Boolean
  Return a.attrs = b.attrs And TuiColorEq(a.fg, b.fg) And TuiColorEq(a.bg, b.bg)
End Function

Public Function TuiCellEq(a, b As TuiCell) As Boolean
  Return a.ch = b.ch And TuiStyleEq(a.style, b.style)
End Function

' --- Theme Palette Helpers ---
Public Function TuiWhite() As TuiColor
  Return TuiRgb(235, 240, 250)
End Function

Public Function TuiGray() As TuiColor
  Return TuiRgb(120, 132, 156)
End Function

Public Function TuiDarkBg() As TuiColor
  Return TuiRgb(18, 22, 34)
End Function

Public Function TuiSurfaceBg() As TuiColor
  Return TuiRgb(28, 34, 50)
End Function

Public Function TuiCyan() As TuiColor
  Return TuiRgb(86, 212, 245)
End Function

Public Function TuiGreen() As TuiColor
  Return TuiRgb(82, 220, 148)
End Function

Public Function TuiYellow() As TuiColor
  Return TuiRgb(250, 206, 84)
End Function

Public Function TuiRed() As TuiColor
  Return TuiRgb(245, 96, 112)
End Function

Public Function TuiPurple() As TuiColor
  Return TuiRgb(182, 128, 252)
End Function

Public Function TuiPollEvent(timeoutMs As Integer) As TuiEvent
  Dim ch As Integer = 0
  Dim k As Integer = TuiTermPollKey(timeoutMs, ByRef ch)
  Dim ev As TuiEvent With ( key = k, ch = ch )
  Return ev
End Function
