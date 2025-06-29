FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TransactionService.API/TransactionService.API.csproj", "TransactionService.API/"]
COPY ["TransactionService.Application/TransactionService.Application.csproj", "TransactionService.Application/"]
COPY ["TransactionService.Domain/TransactionService.Domain.csproj", "TransactionService.Domain/"]
COPY ["TransactionService.Infrastructure/TransactionService.Infrastructure.csproj", "TransactionService.Infrastructure/"]
RUN dotnet restore "TransactionService.API/TransactionService.API.csproj"
COPY . .
WORKDIR "/src/TransactionService.API"
RUN dotnet build "TransactionService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TransactionService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TransactionService.API.dll"] 