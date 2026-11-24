// verbose-tui — Zero-Dependency POSIX Terminal Backend (termios + ioctl + poll + UTF-8/ANSI diff writer)
#define _POSIX_C_SOURCE 200809L
#include <errno.h>
#include <poll.h>
#include <signal.h>
#include <stdbool.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/ioctl.h>
#include <termios.h>
#include <unistd.h>

static struct termios g_orig_termios;
static bool g_raw_active = false;
static volatile sig_atomic_t g_resized = 0;

static void handle_sigwinch(int sig) {
    (void)sig;
    g_resized = 1;
}

static void write_all(const char *buf, size_t len) {
    size_t off = 0;
    while (off < len) {
        ssize_t n = write(STDOUT_FILENO, buf + off, len - off);
        if (n < 0) {
            if (errno == EINTR) continue;
            break;
        }
        off += (size_t)n;
    }
}

void TuiTermShutdown(void) {
    if (!g_raw_active) return;
    const char *restore = "\x1b[0m\x1b[?25h\x1b[?1049l";
    write_all(restore, strlen(restore));
    tcsetattr(STDIN_FILENO, TCSAFLUSH, &g_orig_termios);
    g_raw_active = false;
}

static void handle_sigint(int sig) {
    (void)sig;
    TuiTermShutdown();
    _exit(130);
}

int TuiTermInit(void) {
    if (!isatty(STDIN_FILENO) || !isatty(STDOUT_FILENO)) {
        return 0;
    }
    if (tcgetattr(STDIN_FILENO, &g_orig_termios) != 0) {
        return 0;
    }
    struct termios raw = g_orig_termios;
    raw.c_iflag &= (tcflag_t)~(BRKINT | ICRNL | INPCK | ISTRIP | IXON);
    raw.c_oflag &= (tcflag_t)~(OPOST);
    raw.c_cflag |= (tcflag_t)(CS8);
    raw.c_lflag &= (tcflag_t)~(ECHO | ICANON | IEXTEN | ISIG);
    raw.c_cc[VMIN] = 0;
    raw.c_cc[VTIME] = 0;

    if (tcsetattr(STDIN_FILENO, TCSAFLUSH, &raw) != 0) {
        return 0;
    }
    g_raw_active = true;
    atexit(TuiTermShutdown);
    signal(SIGWINCH, handle_sigwinch);
    signal(SIGINT, handle_sigint);
    signal(SIGTERM, handle_sigint);

    // Enter alternate screen, hide cursor, clear screen
    const char *enter = "\x1b[?1049h\x1b[?25l\x1b[2J\x1b[H";
    write_all(enter, strlen(enter));
    return 1;
}

void TuiTermGetSize(int *outW, int *outH) {
    struct winsize ws;
    if (ioctl(STDOUT_FILENO, TIOCGWINSZ, &ws) == 0 && ws.ws_col > 0 && ws.ws_row > 0) {
        *outW = (int)ws.ws_col;
        *outH = (int)ws.ws_row;
    } else {
        *outW = 80;
        *outH = 24;
    }
}

int TuiTermWasResized(void) {
    if (g_resized) {
        g_resized = 0;
        return 1;
    }
    return 0;
}

void TuiTermWriteBytes(const char *data, int len) {
    if (data && len > 0) {
        write_all(data, (size_t)len);
    }
}

// Encode a Unicode codepoint into UTF-8; returns bytes written (1..4)
int TuiUtf8Encode(int cp, char *dst) {
    uint32_t u = (uint32_t)cp;
    if (u < 0x80) {
        dst[0] = (char)u;
        return 1;
    } else if (u < 0x800) {
        dst[0] = (char)(0xC0 | (u >> 6));
        dst[1] = (char)(0x80 | (u & 0x3F));
        return 2;
    } else if (u < 0x10000) {
        dst[0] = (char)(0xE0 | (u >> 12));
        dst[1] = (char)(0x80 | ((u >> 6) & 0x3F));
        dst[2] = (char)(0x80 | (u & 0x3F));
        return 3;
    } else {
        dst[0] = (char)(0xF0 | (u >> 18));
        dst[1] = (char)(0x80 | ((u >> 12) & 0x3F));
        dst[2] = (char)(0x80 | ((u >> 6) & 0x3F));
        dst[3] = (char)(0x80 | (u & 0x3F));
        return 4;
    }
}

// Key codes matching TuiKey enum in src/tui_types.vb
enum {
    K_NONE = 0,
    K_CHAR = 1,
    K_ENTER = 2,
    K_ESC = 3,
    K_BACKSPACE = 4,
    K_TAB = 5,
    K_UP = 6,
    K_DOWN = 7,
    K_LEFT = 8,
    K_RIGHT = 9,
    K_HOME = 10,
    K_END = 11,
    K_PAGE_UP = 12,
    K_PAGE_DOWN = 13,
    K_DELETE = 14,
    K_CTRL_C = 15,
    K_RESIZE = 16
};

// Decode raw byte buffer (useful for both live stdin & headless unit tests)
int TuiDecodeKeyBytes(const unsigned char *buf, int len, int *outCh) {
    *outCh = 0;
    if (!buf || len <= 0) return K_NONE;

    unsigned char c = buf[0];
    if (c == 3) return K_CTRL_C;
    if (c == '\r' || c == '\n') return K_ENTER;
    if (c == '\t') return K_TAB;
    if (c == 127 || c == 8) return K_BACKSPACE;
    if (c == 0x1b) {
        if (len >= 3 && buf[1] == '[') {
            switch (buf[2]) {
                case 'A': return K_UP;
                case 'B': return K_DOWN;
                case 'C': return K_RIGHT;
                case 'D': return K_LEFT;
                case 'H': return K_HOME;
                case 'F': return K_END;
                case '3': if (len >= 4 && buf[3] == '~') return K_DELETE; break;
                case '5': if (len >= 4 && buf[3] == '~') return K_PAGE_UP; break;
                case '6': if (len >= 4 && buf[3] == '~') return K_PAGE_DOWN; break;
                default: break;
            }
        }
        return K_ESC;
    }
    *outCh = (int)c;
    return K_CHAR;
}

int TuiTermPollKey(int timeoutMs, int *outCh) {
    *outCh = 0;
    if (TuiTermWasResized()) {
        return K_RESIZE;
    }
    struct pollfd pfd;
    pfd.fd = STDIN_FILENO;
    pfd.events = POLLIN;
    pfd.revents = 0;

    int rc = poll(&pfd, 1, timeoutMs);
    if (rc <= 0) {
        if (TuiTermWasResized()) return K_RESIZE;
        return K_NONE;
    }
    unsigned char buf[16];
    ssize_t n = read(STDIN_FILENO, buf, sizeof(buf));
    if (n <= 0) return K_NONE;
    return TuiDecodeKeyBytes(buf, (int)n, outCh);
}

// Format integer decimal into dst; returns bytes written
int TuiFmtInt(int v, char *dst) {
    return snprintf(dst, 16, "%d", v);
}
