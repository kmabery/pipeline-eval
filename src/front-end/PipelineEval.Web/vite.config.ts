/// <reference types="vitest/config" />
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

const repoRoot = path.resolve(fileURLToPath(new URL('.', import.meta.url)), '../../..')

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, repoRoot, '')
  const webPort = Number(env.LOCAL_WEB_PORT) || 5173
  const apiPort = Number(env.LOCAL_API_PORT) || 5101

  return {
    envDir: repoRoot,
    plugins: [react()],
    define: {
      global: 'globalThis',
    },
    server: {
      // Bind to both IPv4 and IPv6 loopback so tools that default to 127.0.0.1
      // (Playwright, curl, Aspire health probes) connect without IPv6-only flakes.
      host: '127.0.0.1',
      port: webPort,
      proxy: {
        '/api': {
          target: `http://127.0.0.1:${apiPort}`,
          changeOrigin: true,
        },
      },
    },
    test: {
      environment: 'jsdom',
      setupFiles: './src/test/setup.ts',
      include: ['tests/unit/**/*.test.{ts,tsx}', 'tests/integration/**/*.test.{ts,tsx}'],
      css: true,
      coverage: {
        provider: 'v8',
        reporter: ['text', 'html'],
        include: ['src/**/*.{ts,tsx}'],
        exclude: ['src/main.tsx', 'src/test/**', 'tests/**'],
      },
    },
  }
})
