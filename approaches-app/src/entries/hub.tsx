import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "../styles/index.css";
import Hub from "../pages/Hub";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <Hub />
  </StrictMode>,
);
