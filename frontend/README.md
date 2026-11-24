# Authdoc frontend

The web client of Authdoc, built with React, TypeScript and Vite.

## How to run

The API must be running first (see the README in the repository root). The pages, the session and the structure are explained in [docs/frontend.md](../docs/frontend.md).

```bash
cd frontend
npm install
npm run dev
```

The app opens at `http://localhost:5173`.

## Commands

| Command | What it does |
|---|---|
| `npm run dev` | Starts the development server with hot reload |
| `npm run build` | Checks the types and builds the production files into `dist/` |
| `npm run lint` | Runs the linter |
| `npm run preview` | Serves the production build locally |
