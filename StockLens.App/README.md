# StockLens.App

Angular 20 dashboard for StockLens. Setup, prerequisites, and how to run the full stack (API +
database + app) live in the [root README](../README.md).

## Quick commands

```bash
npm install      # install dependencies (first run)
npm start        # dev server at http://localhost:4200
npm run build    # production build to dist/
npm test         # unit tests via Karma
```

The app calls the API at `API_BASE` in `src/app/core/config.ts` (default
`http://localhost:5080`), so the StockLens API must be running. See the
[root README](../README.md) for the full setup.
