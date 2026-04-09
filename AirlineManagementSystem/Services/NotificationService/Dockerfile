FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/NotificationService/NotificationService.csproj", "Services/NotificationService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Services/NotificationService/NotificationService.csproj"

COPY . .
WORKDIR "/src/Services/NotificationService"
RUN dotnet build "NotificationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NotificationService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5009
ENV ASPNETCORE_URLS=http://+:5009
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.dll"]


