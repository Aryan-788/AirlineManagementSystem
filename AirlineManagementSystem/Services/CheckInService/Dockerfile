FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/CheckInService/CheckInService.csproj", "Services/CheckInService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Services/CheckInService/CheckInService.csproj"

COPY . .
WORKDIR "/src/Services/CheckInService"
RUN dotnet build "CheckInService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CheckInService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5005
ENV ASPNETCORE_URLS=http://+:5005
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CheckInService.dll"]


