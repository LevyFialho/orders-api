FROM microsoft/aspnetcore:2.0 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/aspnetcore-build:2.0 AS build
WORKDIR /src 
COPY src/OrdersApi.Application/OrdersApi.Application.csproj src/OrdersApi.Application/
COPY src/OrdersApi.Infrastructure/OrdersApi.Infrastructure.csproj src/OrdersApi.Infrastructure/
COPY src/OrdersApi.Domain/OrdersApi.Domain.csproj src/OrdersApi.Domain/
COPY src/OrdersApi.Cqrs/OrdersApi.Cqrs.csproj src/OrdersApi.Cqrs/
COPY src/OrdersApi.IntegrationServices/OrdersApi.IntegrationServices.csproj src/OrdersApi.IntegrationServices/
COPY src/OrdersApi.Contracts/OrdersApi.Contracts.csproj src/OrdersApi.Contracts/
RUN dotnet restore src/OrdersApi.Application/OrdersApi.Application.csproj    
COPY . .
WORKDIR /src/src/OrdersApi.Application
RUN dotnet build OrdersApi.Application.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish OrdersApi.Application.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "OrdersApi.Application.dll"]
