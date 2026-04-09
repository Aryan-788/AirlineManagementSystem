FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/BaggageService/BaggageService.csproj", "Services/BaggageService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Services/BaggageService/BaggageService.csproj"

COPY . .
WORKDIR "/src/Services/BaggageService"
RUN dotnet build "BaggageService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BaggageService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5006
ENV ASPNETCORE_URLS=http://+:5006
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BaggageService.dll"]


