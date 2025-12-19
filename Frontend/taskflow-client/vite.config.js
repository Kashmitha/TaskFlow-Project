import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vitejs.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000,
    open: true,
    proxy: {
      '/api': {
        target: 'https://localhost:5110',
        changeOrigin: true,
        secure: false,
      }
    }
  },
  build: {
    outDir: 'dist',
    sourcemap: true,
    // For better CSP compatibility
    minify: 'terser',
    terserOptions: {
      compress: { 
        drop_console: false,
      },
    },
  },
  // For prevent eval usage
  define: {
    'process.env': {}
  }
})