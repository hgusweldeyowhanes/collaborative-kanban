FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/TaskFlow.Domain/TaskFlow.Domain.csproj", "TaskFlow.Domain/"]
COPY ["src/TaskFlow.Application/TaskFlow.Application.csproj", "TaskFlow.Application/"]
COPY ["src/TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj", "TaskFlow.Infrastructure/"]
COPY ["src/TaskFlow.Presentation/TaskFlow.Presentation.csproj", "TaskFlow.Presentation/"]
RUN dotnet restore "TaskFlow.Presentation/TaskFlow.Presentation.csproj"

COPY src/ .
WORKDIR /src/TaskFlow.Presentation
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "TaskFlow.Presentation.dll"]
