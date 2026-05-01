import type { Config } from "tailwindcss";

const config: Config = {
  content: [
    "./src/pages/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/components/**/*.{js,ts,jsx,tsx,mdx}",
    "./src/app/**/*.{js,ts,jsx,tsx,mdx}",
    // TAMBAHKAN BARIS INI:
    "./src/features/**/*.{js,ts,jsx,tsx,mdx}",
  ],
  theme: {
    extend: {
      colors: {
        foreground: "var(--foreground)",
        primary: "#1a1f3c",
        primaryHover: "#252c52",
        accent: "#c4a661",
        background: "#f8fafc",
        border: "#e2e8f0",
        textPrimary: "#0f172a",
        textSecondary: "#64748b",

      },
    },
  },
  plugins: [],
};
export default config;