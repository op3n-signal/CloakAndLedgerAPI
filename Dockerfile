# Use SDK image for build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy solution and restore dependencies
COPY . ./
RUN dotnet restore Template/Template.csproj

# Build and publish
RUN dotnet publish Template/Template.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /out ./

RUN mkdir -p /app/Downloads
RUN chmod 777 /app/Downloads

EXPOSE 80

ENTRYPOINT ["dotnet", "Template.dll"]