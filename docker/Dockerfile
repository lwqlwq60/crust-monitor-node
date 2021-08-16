############################################ build CrustMonitorNode ############################################

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-crust-monitor-node
WORKDIR /app

COPY ./backend/CrustMonitorNode.sln ./

# copy csproj and restore as distinct layers

COPY ./backend/CrustMonitorNode/*.csproj ./CrustMonitorNode/

RUN dotnet restore

# copy and publish app and libraries

COPY ./backend .

RUN dotnet publish -c Release ./CrustMonitorNode/CrustMonitorNode.csproj -o /publish

############################################ runtime CrustMonitorNode ############################################

# runtime for crust-monitor-node
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime-crust-monitor-node
WORKDIR /app
COPY --from=build-crust-monitor-node /publish ./
ENTRYPOINT ["dotnet", "CrustMonitorNode.dll"]