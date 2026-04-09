FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/FlightService/FlightService.csproj", "Services/FlightService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Services/FlightService/FlightService.csproj"

COPY . .
WORKDIR "/src/Services/FlightService"
RUN dotnet build "FlightService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FlightService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5002
ENV ASPNETCORE_URLS=http://+:5002
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FlightService.dll"]


