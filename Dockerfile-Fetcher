FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY ./PikaFetcher ./PikaFetcher
COPY ./PikaModel ./PikaModel
WORKDIR /src/PikaFetcher
RUN dotnet restore
RUN dotnet build PikaFetcher.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish PikaFetcher.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "PikaFetcher.dll"]
