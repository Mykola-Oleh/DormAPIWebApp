# Стейдж 1: Базовий образ для запуску програми
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Стейдж 2: Збирання проекту (використовуємо SDK)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Копіюємо файл проекту, враховуючи структуру підпапок
COPY ["DormAPIWebApp/DormAPIWebApp.csproj", "DormAPIWebApp/"]
RUN dotnet restore "DormAPIWebApp/DormAPIWebApp.csproj"

# Копіюємо всі інші файли і збираємо проект
COPY . .
WORKDIR "/src/DormAPIWebApp"
RUN dotnet build "DormAPIWebApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Стейдж 3: Публікація готових бінарників
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DormAPIWebApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Стейдж 4: Фінальний образ
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DormAPIWebApp.dll"]