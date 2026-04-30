FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["backend/Hotel.Api/Hotel.Api.csproj", "backend/Hotel.Api/"]
RUN dotnet restore "backend/Hotel.Api/Hotel.Api.csproj"

COPY . .
RUN dotnet publish "backend/Hotel.Api/Hotel.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Hotel.Api.dll"]
