FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/AgentService/AgentService.csproj", "Services/AgentService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Services/AgentService/AgentService.csproj"

COPY . .
WORKDIR "/src/Services/AgentService"
RUN dotnet build "AgentService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AgentService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5008
ENV ASPNETCORE_URLS=http://+:5008
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AgentService.dll"]


