import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";

// Alcance de cobertura definido en docs/SQA-Plan.md (seccion 7):
// capa de acceso a la API, autenticacion/rutas protegidas y componentes comunes reutilizables.
// Las paginas (vistas) se validan con las pruebas de integracion del backend y pruebas manuales;
// quedan registradas como deuda tecnica en docs/Reporte-Mejora.md.
export default defineConfig({
  plugins: [react()],
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/test/setup.ts"],
    include: ["src/**/*.test.{ts,tsx}"],
    reporters: ["default", "junit"],
    outputFile: { junit: "coverage/junit.xml" },
    coverage: {
      provider: "v8",
      reporter: ["text", "text-summary", "lcov", "cobertura", "json-summary"],
      reportsDirectory: "coverage",
      include: [
        "src/api/**/*.ts",
        "src/auth/**/*.tsx",
        "src/components/common/**/*.tsx",
      ],
      exclude: ["src/test/**", "**/*.test.*"],
      thresholds: {
        lines: 60,
        statements: 60,
        functions: 60,
        branches: 60,
      },
    },
  },
});
