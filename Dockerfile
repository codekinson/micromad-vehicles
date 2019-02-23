FROM microsoft/dotnet:2.1-sdk AS build
ARG configuration=Release
WORKDIR /src
COPY ./src/Vehicles.RestResources/*.csproj ./Vehicles.RestResources/
COPY ./src/Vehicles.Services/*.csproj ./Vehicles.Services/
RUN dotnet restore ./Vehicles.Services/Vehicles.Services.csproj
COPY ./src ./
RUN dotnet publish ./Vehicles.Services/Vehicles.Services.csproj -c ${configuration} -o out

FROM microsoft/dotnet:2.1-aspnetcore-runtime AS runtime
WORKDIR /app
COPY --from=build /src/Vehicles.Services/out ./
ENTRYPOINT ["dotnet", "Vehicles.Services.dll"]