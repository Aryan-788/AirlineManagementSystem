FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/IdentityService/IdentityService.csproj", "Services/IdentityService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Services/IdentityService/IdentityService.csproj"

COPY . .
WORKDIR "/src/Services/IdentityService"
RUN dotnet build "IdentityService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IdentityService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5001
ENV ASPNETCORE_URLS=http://+:5001
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.dll"]


