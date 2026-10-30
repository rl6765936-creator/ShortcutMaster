import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// GitHub Pages serves this project at https://<user>.github.io/ShortcutMaster/.
// Override with BASE_PATH (for example BASE_PATH=/ for a custom domain).
export default defineConfig({
  base: process.env.BASE_PATH ?? '/ShortcutMaster/',
  plugins: [react()],
  build: { target: 'es2022', sourcemap: false },
});
