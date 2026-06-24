import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import "../styles/index.css";
import TwoLedgers from "../pages/TwoLedgers";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <TwoLedgers />
  </StrictMode>,
);
