import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import path from "node:path";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    // Matches the API's Cors:AllowedOrigin default (http://localhost:3000)
    // and infra/docker-compose.yml's web service port mapping.
    port: 3000,
  },
  preview: {
    port: 3000,
  },
});
