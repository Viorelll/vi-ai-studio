FROM node:22-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

# Vite inlines VITE_* env vars into the built JS at build time (not runtime,
# unlike Next's NEXT_PUBLIC_* vars) -- pass it as a build ARG.
ARG VITE_API_BASE_URL=http://localhost:5081
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

COPY . .
RUN npm run build

# Static output only -- no Node runtime needed to serve a CSR SPA.
FROM node:22-alpine
WORKDIR /app
RUN npm install -g serve

COPY --from=build /app/dist ./dist

EXPOSE 3000
CMD ["serve", "-s", "dist", "-l", "3000"]
