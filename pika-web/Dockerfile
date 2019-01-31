FROM microsoft/dotnet:2.2-sdk
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

EXPOSE 5000

# copy and build everything else
COPY . ./
RUN dotnet publish PikaWeb.csproj -c Release -o out
ENTRYPOINT ["dotnet", "out/PikaWeb.dll"]