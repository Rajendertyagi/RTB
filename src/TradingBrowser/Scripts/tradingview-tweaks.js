// 1. Hide Scrollbars globally for a cleaner chart experience
const style = document.createElement('style');
style.innerHTML = `
    ::-webkit-scrollbar { display: none !important; width: 0 !important; height: 0 !important; }
    body { -ms-overflow-style: none; scrollbar-width: none; }
`;
document.head.appendChild(style);

// 2. Forward Renderer Errors & Unhandled Promises to C#
window.addEventListener('error', function (e) {
    window.chrome.webview.postMessage('LOG:JS_ERROR:' + e.message + ' at ' + e.filename + ':' + e.lineno);
});

window.addEventListener('unhandledrejection', function (e) {
    window.chrome.webview.postMessage('LOG:JS_PROMISE:' + e.reason);
});
