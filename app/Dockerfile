# why the fucking hell dont we have build environment variables?! fucking shit
ARG API_ENDPOINT
ARG API_KEY
ARG SENTRY_AUTH_TOKEN
FROM node:hydrogen-alpine AS builder
ENV API_ENDPOINT=${API_ENDPOINT}
ENV API_KEY=${API_KEY}
ENV SENTRY_AUTH_TOKEN=${SENTRY_AUTH_TOKEN}
WORKDIR /app
COPY package*.json .
RUN npm ci

COPY . .
RUN npx svelte-kit sync
RUN npm run build --omit=dev
RUN npm prune --omit=dev

FROM node:hydrogen-alpine
ENV API_ENDPOINT=${API_ENDPOINT}
ENV API_KEY=${API_KEY}
ENV SENTRY_AUTH_TOKEN=${SENTRY_AUTH_TOKEN}
WORKDIR /app
COPY --from=builder /app/build build
COPY --from=builder /app/node_modules node_modules/
COPY package.json .
EXPOSE 3000
ENV NODE_ENV=production
CMD [ "node", "build" ]