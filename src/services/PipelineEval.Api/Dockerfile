FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Build from repo root (see reusable-cd-deploy.yml) so ProjectReference to PipelineEval.Observability resolves.
WORKDIR /src
COPY src/services/PipelineEval.Api/PipelineEval.Api.csproj services/PipelineEval.Api/
COPY src/libraries/Observability/PipelineEval.Observability/PipelineEval.Observability.csproj libraries/Observability/PipelineEval.Observability/
RUN dotnet restore services/PipelineEval.Api/PipelineEval.Api.csproj
COPY src/ .
RUN dotnet publish services/PipelineEval.Api/PipelineEval.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PipelineEval.Api.dll"]
