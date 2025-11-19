# Use SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and restore dependencies
COPY . ./
RUN dotnet restore EncompassAPI/EncompassAPI.csproj

# Build and publish
RUN dotnet publish EncompassAPI/EncompassAPI.csproj -c Release -o /out

RUN mkdir -p /app/Downloads
RUN chmod 777 /app/Downloads

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out ./

# Create the .aws directory in the container
RUN mkdir -p /root/.aws \
    && chmod 777 /root/.aws

EXPOSE 80

ENTRYPOINT ["dotnet", "EncompassAPI.dll"]