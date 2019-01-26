FROM microsoft/dotnet:2.2-sdk
WORKDIR /app

# copy csproj and restore as distinct layers
COPY *.csproj ./
RUN dotnet restore

# copy and build everything else
COPY . ./
RUN dotnet publish PikaFetcher.sln -c Release -o out
ENTRYPOINT ["dotnet", "out/PikaFetcher.dll"]