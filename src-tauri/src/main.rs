#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use tauri::{Manager, WebviewWindow};
use url::Url;

#[tauri::command]
async fn navigate(webview: WebviewWindow, url: String) -> Result<(), String> {
    let parsed_url = Url::parse(&url).map_err(|e| e.to_string())?;

    if let Some(browser) = webview.app_handle().get_webview_window("browser") {
        browser.navigate(parsed_url).map_err(|e| e.to_string())?;
    } else {
        tauri::WebviewWindowBuilder::new(
            webview.app_handle(),
            "browser",
            tauri::WebviewUrl::External(parsed_url),
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
