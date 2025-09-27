FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app
COPY *.sln .
COPY OfficePlanner/*.csproj ./OfficePlanner/

RUN dotnet restore
RUN cd OfficePlanner && dotnet restore

COPY . .
WORKDIR /app/OfficePlanner
RUN dotnet publish -c Release -o /pub --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /pub ./
ENTRYPOINT ["dotnet", "OfficePlanner.dll"]
