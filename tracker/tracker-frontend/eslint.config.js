import globals from "globals";
import pluginJs from "@typescript-eslint/eslint-plugin";
import tsParser from "@typescript-eslint/parser";
import pluginReact from "eslint-plugin-react";

export default [
  {
    files: ["**/*.{js,mjs,cjs,ts,jsx,tsx}"],
    languageOptions: {
      globals: globals.browser,
      parser: tsParser,
      parserOptions: {
        ecmaVersion: "latest",
        sourceType: "module",
        ecmaFeatures: {
          jsx: true,
        },
      },
    },
    plugins: {
      "@typescript-eslint": pluginJs,
      "react": pluginReact,
    },
    rules: {
      // Your custom rules here
    },
  },
];
export default {
  plugins: {
    tailwindcss: {},
    autoprefixer: {},
  },
}

