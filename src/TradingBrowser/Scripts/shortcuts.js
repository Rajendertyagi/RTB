window.addEventListener('keydown', function (e) {
    // Ctrl+T, Ctrl+W, Ctrl+L
    if (e.ctrlKey && ['t', 'w', 'l'].includes(e.key.toLowerCase())) {
        e.preventDefault();
        window.chrome.webview.postMessage('SHORTCUT:CTRL_' + e.key.toUpperCase());
    }
    // Ctrl+Tab / Ctrl+Shift+Tab
    else if (e.ctrlKey && e.key === 'Tab') {
        e.preventDefault();
        window.chrome.webview.postMessage(e.shiftKey ? 'SHORTCUT:CTRL_SHIFT_TAB' : 'SHORTCUT:CTRL_TAB');
    }
    // Ctrl+1 through Ctrl+9
    else if (e.ctrlKey && e.key >= '1' && e.key <= '9') {
        e.preventDefault();
        window.chrome.webview.postMessage('SHORTCUT:CTRL_NUM_' + e.key);
    }
    // F11 (Fullscreen), F12 (DevTools), F5 (Reload)
    else if (['F11', 'F12', 'F5'].includes(e.key)) {
        e.preventDefault();
        window.chrome.webview.postMessage('SHORTCUT:' + e.key);
    }
    // Ctrl+Shift+T (Reopen closed tab)
    else if (e.ctrlKey && e.shiftKey && e.key.toLowerCase() === 't') {
        e.preventDefault();
        window.chrome.webview.postMessage('SHORTCUT:CTRL_SHIFT_T');
    }
});
