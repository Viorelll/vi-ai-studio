# Vi - AI Studio — web

Client-side SPA built with Vite + React + TypeScript. Talks to the .NET API
in `apps/api` over plain `fetch` (see `src/lib/api-client.ts`) -- there's no
server-side rendering here, everything renders in the browser.

## Stack

- **React + Vite + TypeScript**
- **Tailwind CSS v4** + **shadcn/ui** (Base UI primitives) for components
- **TanStack Query** for server data (`src/hooks/*`) + **Zustand** for the one
  piece of client-only state (the accent color, `src/store/accent-store.ts`)
- **React Hook Form + Zod** for validated forms (AI model config, launch)
- **react-router-dom** for client-side routing (`src/App.tsx`)

## Getting started

```bash
npm install
npm run dev
```

Open [http://localhost:3000](http://localhost:3000). The API is expected at
`http://localhost:5081` by default -- see `.env.example` (`VITE_API_BASE_URL`)
to point elsewhere. Copy it to `.env.local` to override locally.

Routes live under `src/pages/`, one file per screen; `src/App.tsx` wires them
to URLs. Server data hooks live under `src/hooks/`, grouped by API resource.

## Other scripts

```bash
npm run build     # tsc -b && vite build -> dist/
npm run preview   # serve the production build locally
npm run lint       # eslint .
```
