FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/RewardService/RewardService.csproj", "Services/RewardService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Services/RewardService/RewardService.csproj"

COPY . .
WORKDIR "/src/Services/RewardService"
RUN dotnet build "RewardService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "RewardService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5007
ENV ASPNETCORE_URLS=http://+:5007
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RewardService.dll"]


