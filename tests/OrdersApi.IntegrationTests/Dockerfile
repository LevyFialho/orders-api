FROM microsoft/aspnetcore-build:2.0 AS build 
COPY . ./app
WORKDIR /app/tests/OrdersApi.ApplicationTests
COPY Nuget.config ./ 
RUN dotnet restore --configfile ./Nuget.config  