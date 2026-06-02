FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MS-ApiGateway.csproj", "./"]
RUN dotnet restore "MS-ApiGateway.csproj"
COPY . .
RUN dotnet build "MS-ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MS-ApiGateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MS-ApiGateway.dll", "--urls", "http://0.0.0.0:5000"]
