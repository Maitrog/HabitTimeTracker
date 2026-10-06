FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY HabitTimeTracker/HabitTimeTracker.csproj HabitTimeTracker/
RUN dotnet restore HabitTimeTracker/HabitTimeTracker.csproj
COPY HabitTimeTracker/ HabitTimeTracker/
RUN dotnet publish HabitTimeTracker/HabitTimeTracker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
RUN apt-get update && apt-get install -y --no-install-recommends libfontconfig1 fonts-dejavu-core \
    && rm -rf /var/lib/apt/lists/*
ENTRYPOINT ["dotnet", "HabitTimeTracker.dll"]
