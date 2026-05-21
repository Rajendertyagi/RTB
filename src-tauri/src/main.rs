#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use tauri::{Manager, Window};

#[tauri::command]
async fn navigate(window: Window, url: String) -> Result<(), String> {
    let app = window.app_handle();
    
    // Get or create browser window
    if let Some(browser) = app.get_webview_window("browser") {
        browser.navigate(&url).map_err(|e| e.to_string())?;
    } else {
        tauri::WebviewWindowBuilder::new(
            &app,
            "browser",
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
        .invoke_handler(tauri::generate_handler![navigate])
        .run(tauri::generate_context!())
        .expect("error");
}
