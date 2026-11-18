# --- build ---
FROM node:22-alpine AS build
WORKDIR /app
COPY package.json ./
# "npm install" (e não "npm ci"): os binários nativos opcionais são resolvidos para a plataforma do contêiner.
RUN npm install --no-audit --no-fund
COPY . .
ARG VITE_API_URL=http://localhost:8080
ENV VITE_API_URL=$VITE_API_URL
RUN npm run build

# --- runtime ---
FROM nginx:1.27-alpine
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY nginx-security-headers.conf /etc/nginx/snippets/security-headers.conf
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
