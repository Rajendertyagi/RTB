#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use tauri::{Manager, WebviewWindow};

// ✅ Verified command: accepts WebviewWindow parameter [[2]]
#[tauri::command]
async fn navigate(webview: WebviewWindow, url: String) -> Result<(), String> {
    // Get or create browser window
    if let Some(browser) = webview.app_handle().get_webview_window("browser") {
        browser.navigate(&url).map_err(|e| e.to_string())?;
    } else {
        // ✅ Verified API: WebviewWindowBuilder::new [[1]]
        tauri::WebviewWindowBuilder::new(
            webview.app_handle(),
            "browser",  // unique label
            tauri::WebviewUrl::External(url.parse().map_err(|e| e.to_string())?),
        )
        .title("TB Browser")
        .inner_size(900.0, 600.0)
        .build()
        .map_err(|e| e.to_string())?;
    }
    Ok(())
}

fn main() {
    tauri::Builder::default()
        // ✅ Verified: generate_handler! macro [[2]]
        .invoke_handler(tauri::generate_handler![navigate])
        .run(tauri::generate_context!())
        .expect("error");
}
