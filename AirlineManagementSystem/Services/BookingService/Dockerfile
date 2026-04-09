FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/BookingService/BookingService.csproj", "Services/BookingService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Services/BookingService/BookingService.csproj"

COPY . .
WORKDIR "/src/Services/BookingService"
RUN dotnet build "BookingService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BookingService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5003
ENV ASPNETCORE_URLS=http://+:5003
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BookingService.dll"]


