FROM mcr.microsoft.com/dotnet/sdk:8.0 as build

WORKDIR /workspace
COPY . .

RUN dotnet restore
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:8.0

RUN apt-get update && apt-get install -y --no-install-recommends fonts-noto-cjk

WORKDIR /app
COPY --from=build /workspace/out .

ENTRYPOINT [ "dotnet", "MisskeyEmojiNotify.dll" ]