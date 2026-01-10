
import { createRoot } from "react-dom/client";
import App from "./app/App.tsx";
import "./styles/index.css";

// WebView2 loads the app as https://.../index.html.
// Normalize the initial URL BEFORE the router mounts, otherwise the first render can be blank.
try {
  const p = window.location.pathname;
  if (p === "/index.html" || p.endsWith("/index.html")) {
    window.history.replaceState(null, "", "/");
  }
} catch {
  // ignore
}

createRoot(document.getElementById("root")!).render(<App />);
  