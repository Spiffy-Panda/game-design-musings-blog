import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "../styles/index.css";
import KnownWar from "../pages/KnownWar";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <KnownWar />
  </StrictMode>,
);
