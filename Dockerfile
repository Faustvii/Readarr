# syntax=docker/dockerfile:1

# Using a more recent Alpine base if possible, or stick to 3.22 if required
# Consider mcr.microsoft.com/dotnet/runtime-deps:6.0-alpine if you want official .NET runtime dependencies
FROM docker.io/library/alpine:3.22

ARG TARGETARCH
ARG VENDOR
ARG VERSION

ENV COMPlus_EnableDiagnostics=0 \
    READARR__UPDATE__BRANCH=develop

USER root
WORKDIR /app

RUN \
    apk add --no-cache \
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

# COPY the pre-built binaries from the build context
# The 'docker-context' will contain the contents of 'linux-musl-x64/publish'
# and also your entrypoint.sh
COPY . /app/bin/

# Remove updater if not needed (original Dockerfile did this)
# Assuming Readarr.Update is part of the copied binaries
RUN rm -f /app/bin/Readarr.Update

# Create package_info dynamically during the Docker build
# The original 'curl' command downloaded the binaries and created package_info
# Now, the binaries are copied, so we create package_info based on build args/env vars
RUN printf "UpdateMethod=docker\nBranch=%s\nPackageVersion=%s\nPackageAuthor=[%s](https://github.com/%s)\n" "${READARR__UPDATE__BRANCH}" "${VERSION}" "${VENDOR}" "${VENDOR}" > /app/package_info

# Original Dockerfile also copied . to /
# This might overwrite /app/bin with a . (repo root) that is unexpected
# Ensure your 'COPY .' is specific to what you need.
# If entrypoint.sh is at the repo root and you want it in the image root, this is fine.
# If entrypoint.sh is expected to be in /app/bin, then the previous COPY . /app/bin/ handles it.
# Assuming entrypoint.sh is placed in /docker-context during 'Prepare Docker Context' step.
COPY entrypoint.sh /entrypoint.sh

USER nobody:nogroup
WORKDIR /config
VOLUME ["/config"]

ENTRYPOINT ["/usr/bin/catatonit", "--", "/entrypoint.sh"]