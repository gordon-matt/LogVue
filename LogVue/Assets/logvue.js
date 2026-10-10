(function () {
    'use strict';

    const storage = {
        get(key) { try { return localStorage.getItem(key); } catch { return null; } },
        set(key, value) { try { localStorage.setItem(key, value); } catch { /* private mode */ } }
    };

    const isTyping = target =>
        !!target && (target.isContentEditable || /^(INPUT|TEXTAREA|SELECT)$/.test(target.tagName));

    let keyHandler = null;

    window.logVue = {
        /** Wires keyboard shortcuts and reports what the circuit can't know: the browser's zone and preferences. */
        init(dotnet) {
            document.getElementById('lv-boot')?.remove();

            if (keyHandler) {
                document.removeEventListener('keydown', keyHandler);
            }

            keyHandler = e => {
                if (e.defaultPrevented || e.ctrlKey || e.metaKey || e.altKey) {
                    return;
                }

                if (e.key === 'Escape') {
                    // A dialog handles its own dismissal.
                    if (!document.querySelector('.lv-modal')) {
                        dotnet.invokeMethodAsync('OnShortcut', 'Escape');
                    }
                    return;
                }

                if (isTyping(e.target)) {
                    return;
                }

                if (e.key === '/') {
                    e.preventDefault();
                    const search = document.getElementById('lv-search');
                    search?.focus();
                    search?.select();
                } else if (e.key === 'j' || e.key === 'k') {
                    e.preventDefault();
                    dotnet.invokeMethodAsync('OnShortcut', e.key);
                }
            };

            document.addEventListener('keydown', keyHandler);

            let timeZone = null;
            try { timeZone = Intl.DateTimeFormat().resolvedOptions().timeZone; } catch { /* very old browser */ }

            return {
                timeZone,
                utc: storage.get('logvue.utc') === '1',
                theme: document.documentElement.getAttribute('data-theme') || 'light'
            };
        },

        setTheme(theme) {
            document.documentElement.setAttribute('data-theme', theme);
            storage.set('logvue.theme', theme);
        },

        setUtc(utc) {
            storage.set('logvue.utc', utc ? '1' : '0');
        },

        async copy(text) {
            try {
                await navigator.clipboard.writeText(text);
                return true;
            } catch {
                // The async clipboard API needs a secure context; plain-http intranet hosts are common.
                const area = document.createElement('textarea');
                area.value = text;
                area.style.position = 'fixed';
                area.style.opacity = '0';
                document.body.appendChild(area);
                area.select();
                const copied = document.execCommand('copy');
                area.remove();
                return copied;
            }
        },

        reveal(id) {
            document.getElementById(id)?.scrollIntoView({ block: 'nearest' });
        },

        /**
         * Watches the top and bottom of the entry list: the bottom to load older entries before the
         * reader gets there, the top so live tail only inserts rows while the reader is at the top.
         */
        observeList(scroll, top, bottom, dotnet) {
            let topVisible = true;

            const topObserver = new IntersectionObserver(entries => {
                const visible = entries[entries.length - 1].isIntersecting;
                if (visible !== topVisible) {
                    topVisible = visible;
                    dotnet.invokeMethodAsync('OnTopVisibilityChanged', visible);
                }
            }, { root: scroll });

            const bottomObserver = new IntersectionObserver(entries => {
                if (entries[entries.length - 1].isIntersecting) {
                    dotnet.invokeMethodAsync('OnBottomVisible');
                }
            }, { root: scroll, rootMargin: '0px 0px 800px 0px' });

            topObserver.observe(top);
            bottomObserver.observe(bottom);

            return {
                /** An observer only reports changes; re-observing reports the current state, for after a load. */
                recheck() {
                    bottomObserver.unobserve(bottom);
                    bottomObserver.observe(bottom);
                },
                scrollToTop() {
                    scroll.scrollTo({ top: 0, behavior: 'smooth' });
                },
                dispose() {
                    topObserver.disconnect();
                    bottomObserver.disconnect();
                }
            };
        }
    };
})();
