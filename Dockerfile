# One image for the whole app: the API plus the React UI, served by nginx.
#   5080 - backend API (Swagger at /swagger, health at /health)
#   5173 - React UI
#   8080 - everything on one port (UI, /api, /health, /swagger), for hosting (render.yaml)
# The ports match local development, so the Swagger "UI" button and every URL in README.md
# work unchanged. The UI port also proxies /api to the backend, so the app calls it same-origin.
# Tests run in CI (.github/workflows/ci.yml) before this image is built.

# ---- Frontend ----
FROM node:24-slim AS frontend
WORKDIR /src
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci
COPY frontend/ ./
RUN npm run build

# ---- Backend ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS backend
WORKDIR /src
COPY backend/Directory.Build.props backend/.editorconfig ./
COPY backend/Rubik.Core/Rubik.Core.csproj Rubik.Core/
COPY backend/Rubik.Api/Rubik.Api.csproj Rubik.Api/
RUN dotnet restore Rubik.Api/Rubik.Api.csproj
COPY backend/Rubik.Core/ Rubik.Core/
COPY backend/Rubik.Api/ Rubik.Api/
RUN dotnet publish Rubik.Api/Rubik.Api.csproj -c Release --no-restore -o /out

# ---- Runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends nginx \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=backend /out ./
COPY --from=frontend /src/dist /srv/frontend
COPY docker/nginx.conf /etc/nginx/nginx.conf
COPY docker/entrypoint.sh /entrypoint.sh
# Strip CRLF in case the script was checked out on Windows.
RUN sed -i 's/\r$//' /entrypoint.sh && chmod +x /entrypoint.sh

ENV ASPNETCORE_HTTP_PORTS=5080
EXPOSE 5080 5173 8080

USER $APP_UID
ENTRYPOINT ["/entrypoint.sh"]
