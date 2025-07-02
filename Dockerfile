# syntax=docker/dockerfile:1

# Stage 1: Builder
FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS builder

ARG VERSION
ARG BRANCH=develop
ARG BUILD_CONFIGURATION=Release

# Install build and runtime dependencies
RUN apk add --no-cache \
    bash \
    git \
    icu-libs \
    nodejs \
    yarn \
    coreutils

WORKDIR /src

# Copy source code
COPY . .

# Update version information
RUN if [ -n "$VERSION" ]; then \
        sed -i "s/<AssemblyVersion>[0-9.*]\+<\/AssemblyVersion>/<AssemblyVersion>$VERSION<\/AssemblyVersion>/g" src/Directory.Build.props && \
        sed -i "s/<AssemblyConfiguration>[\$()A-Za-z-]\+<\/AssemblyConfiguration>/<AssemblyConfiguration>${BRANCH}<\/AssemblyConfiguration>/g" src/Directory.Build.props && \
        sed -i "s/<string>10.0.0.0<\/string>/<string>$VERSION<\/string>/g" distribution/osx/Readarr.app/Contents/Info.plist; \
    fi

# Build everything (backend, frontend, packages)
RUN chmod +x build.sh && ./build.sh --all

# Stage 2: Final Runtime Image
FROM docker.io/library/alpine:3.22

ARG TARGETARCH
ARG VENDOR
ARG VERSION
ARG BRANCH=develop
ARG PackageOwner=faustvii
ARG PackageRepo=readarr

ENV COMPlus_EnableDiagnostics=0 \
    READARR__UPDATE__BRANCH=${BRANCH}

USER root
WORKDIR /app

# Install runtime dependencies
RUN apk add --no-cache \
        bash \
        ca-certificates \
        catatonit \
        coreutils \
        icu-libs \
        libintl \
        nano \
        sqlite-libs \
        tzdata \
    && mkdir -p /app/bin \
    && chown -R root:root /app && chmod -R 755 /app

# Copy the packaged application from the builder stage
COPY --from=builder /src/_artifacts/linux-musl-x64/net6.0/Readarr /app/bin/

# Remove updater if not needed
RUN rm -rf /app/bin/Readarr.Update

# Create package_info dynamically
RUN printf "UpdateMethod=docker\nBranch=%s\nPackageVersion=%s\nPackageAuthor=[%s](https://github.com/%s)\nPackageOwner=%s\nPackageRepo=%s\n" "${READARR__UPDATE__BRANCH}" "${VERSION}" "${VENDOR}" "${VENDOR}" "${PackageOwner}" "${PackageRepo}" > /app/package_info

# Copy entrypoint script
COPY entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh

USER nobody:nogroup
WORKDIR /config
VOLUME ["/config"]

ENTRYPOINT ["/usr/bin/catatonit", "--", "/entrypoint.sh"]