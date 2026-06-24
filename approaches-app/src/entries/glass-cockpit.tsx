import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "../styles/index.css";
import GlassCockpit from "../pages/GlassCockpit";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <GlassCockpit />
  </StrictMode>,
);
