FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Services/PaymentService/PaymentService.csproj", "Services/PaymentService/"]
COPY ["Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "Services/PaymentService/PaymentService.csproj"

COPY . .
WORKDIR "/src/Services/PaymentService"
RUN dotnet build "PaymentService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PaymentService.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5004
ENV ASPNETCORE_URLS=http://+:5004
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PaymentService.dll"]


