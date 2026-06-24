import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { resolve } from "node:path";

// Resolve entry HTML files relative to this config's folder (Node 20.11+ provides
// import.meta.dirname).
const r = (p: string) => resolve(import.meta.dirname, p);

// Relative base so the built pages work both locally (served at /) and on GitHub Pages
// (served under /game-design-musings-blog/). Multi-page: one HTML entry per page; Vite
// preserves the folder structure in dist/, which is then copied into
// site/musings/<slug>/approaches/ by utils/python/build_site.py.
export default defineConfig({
  base: "./",
  plugins: [react(), tailwindcss()],
  build: {
    outDir: "dist",
    emptyOutDir: true,
    rollupOptions: {
      input: {
        hub: r("index.html"),
        "two-ledgers": r("two-ledgers/index.html"),
        "known-war": r("known-war/index.html"),
        "glass-cockpit": r("glass-cockpit/index.html"),
      },
    },
  },
});
