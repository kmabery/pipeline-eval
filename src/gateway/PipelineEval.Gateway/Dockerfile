FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
# Build from repo root (see reusable-cd-deploy.yml) so ProjectReference to PipelineEval.Observability resolves.
WORKDIR /src
COPY src/gateway/PipelineEval.Gateway/PipelineEval.Gateway.csproj gateway/PipelineEval.Gateway/
COPY src/libraries/Observability/PipelineEval.Observability/PipelineEval.Observability.csproj libraries/Observability/PipelineEval.Observability/
RUN dotnet restore gateway/PipelineEval.Gateway/PipelineEval.Gateway.csproj
COPY src/ .
RUN dotnet publish gateway/PipelineEval.Gateway/PipelineEval.Gateway.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PipelineEval.Gateway.dll"]
