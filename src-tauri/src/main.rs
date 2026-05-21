#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use tauri::{Manager, WebviewWindow, PhysicalPosition, PhysicalSize};
use url::Url;

#[tauri::command]
async fn navigate(webview: WebviewWindow, url: String) -> Result<(), String> {
    let parsed = Url::parse(&url).map_err(|e| e.to_string())?;
    let app = webview.app_handle();

    if let Some(browser) = app.get_webview_window("browser") {
        browser.navigate(parsed).map_err(|e| e.to_string())?;
    } else {
        // Get toolbar position
        let toolbar_pos = webview.outer_position().map_err(|e| e.to_string())?;
        let toolbar_size = webview.outer_size().map_err(|e| e.to_string())?;

        tauri::WebviewWindowBuilder::new(
            app,
            "browser",
            tauri::WebviewUrl::External(parsed),
        )
        .title("TB Browser")
        .decorations(false) // No title bar
        .always_on_top(true)
        .position(
            toolbar_pos.x as f64,
            (toolbar_pos.y + toolbar_size.height as i32) as f64,
        )
        .inner_size(900.0, 600.0)
        .build()
        .map_err(|e| e.to_string())?;
    }
    Ok(())
}

#[tauri::command]
async fn go_back(webview: WebviewWindow) -> Result<(), String> {
    if let Some(browser) = webview.app_handle().get_webview_window("browser") {
        // Simple reload workaround
        let _ = browser.eval("window.history.back()");
    }
    Ok(())
}

#[tauri::command]
async fn go_forward(webview: WebviewWindow) -> Result<(), String> {
    if let Some(browser) = webview.app_handle().get_webview_window("browser") {
        let _ = browser.eval("window.history.forward()");
    }
    Ok(())
}

#[tauri::command]
async fn reload(webview: WebviewWindow) -> Result<(), String> {
    if let Some(browser) = webview.app_handle().get_webview_window("browser") {
        let _ = browser.eval("window.location.reload()");
    }
    Ok(())
}

fn main() {
    tauri::Builder::default()
        .invoke_handler(tauri::generate_handler![navigate, go_back, go_forward, reload])
        .run(tauri::generate_context!())
        .expect("error");
}
