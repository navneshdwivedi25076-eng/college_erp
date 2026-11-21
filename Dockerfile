# ---------- 1. Build the React 19 frontend ----------
FROM node:22-alpine AS web
WORKDIR /web
COPY frontend/package*.json ./
RUN npm install
COPY frontend/ ./
RUN npm run build

# ---------- 2. Build & publish the .NET 10 API ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS api
WORKDIR /src
COPY backend/ ./
RUN dotnet publish src/CollegeErp.Api/CollegeErp.Api.csproj -c Release -o /app

# ---------- 3. Runtime: API serves the SPA from wwwroot ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=api /app ./
COPY --from=web /web/dist ./wwwroot
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "CollegeErp.Api.dll"]
