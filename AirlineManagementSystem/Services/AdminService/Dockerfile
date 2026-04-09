FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/AdminService/AdminService.csproj", "Services/AdminService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Services/AdminService/AdminService.csproj"

COPY . .
WORKDIR "/src/Services/AdminService"
RUN dotnet build "AdminService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AdminService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5010
ENV ASPNETCORE_URLS=http://+:5010
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AdminService.dll"]


