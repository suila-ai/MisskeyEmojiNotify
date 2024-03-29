FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 as build
ARG TARGETARCH

WORKDIR /workspace
COPY . .

RUN dotnet restore -a $TARGETARCH
RUN dotnet publish -c Release -o out -a $TARGETARCH

FROM mcr.microsoft.com/dotnet/runtime:8.0

ENV TZ=Asia/Tokyo

RUN apt-get update && apt-get install -y --no-install-recommends fonts-noto-cjk

WORKDIR /app
COPY --from=build /workspace/out .

ENTRYPOINT [ "dotnet", "MisskeyEmojiNotify.dll" ]
