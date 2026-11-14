import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // The API has no CORS configuration, so the browser cannot call it from
    // another port. In development Vite forwards every /api request to the API.
    proxy: {
      '/api': 'http://localhost:5297',
    },
  },
})
