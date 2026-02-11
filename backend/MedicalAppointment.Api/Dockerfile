# syntax=docker/dockerfile:1.7

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["MedicalAppointment.Api/MedicalAppointment.Api.csproj", "MedicalAppointment.Api/"]
COPY ["MedicalAppointment.Infrastructure/MedicalAppointment.Infrastructure.csproj", "MedicalAppointment.Infrastructure/"]
COPY ["MedicalAppointment.Domain/MedicalAppointment.Domain.csproj", "MedicalAppointment.Domain/"]

RUN dotnet restore "MedicalAppointment.Api/MedicalAppointment.Api.csproj"

COPY . .
WORKDIR /src/MedicalAppointment.Api
RUN dotnet publish "MedicalAppointment.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MedicalAppointment.Api.dll"]
