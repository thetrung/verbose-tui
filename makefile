VERBOSE_BIN ?= verbose
BACKEND := csrc/tui_term.c

.PHONY: all demos run-dashboard test clean

all: demos

demos:
	@mkdir -p build
	$(VERBOSE_BIN) demos/01_dashboard.vb -I src $(BACKEND) --no-run -o build/01_dashboard
	@echo "[OK] Built TUI demo -> build/01_dashboard"

run-dashboard: demos
	./build/01_dashboard

test:
	$(VERBOSE_BIN) test tests/test_tui.vb -I src $(BACKEND)
	$(VERBOSE_BIN) demos/01_dashboard.vb -I src $(BACKEND) --no-run -o build/01_dashboard
	./build/01_dashboard --snapshot

clean:
	rm -rf build demos/*.ll tests/*.ll .vpkg
