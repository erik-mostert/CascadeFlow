import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  build: {
    outDir: '../Cascade.Collector/wwwroot',
    emptyOutDir: true
  },
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5100',
        changeOrigin: true
      },
      '/hubs': {
        target: 'http://localhost:5100',
        changeOrigin: true,
        ws: true
      }
    }
  }
})
