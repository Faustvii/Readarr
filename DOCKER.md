# Docker Build Guide

This document explains how to build and use Readarr Docker images.

## Overview

The Dockerfile uses a multi-stage build process that:
1. Builds the .NET backend
2. Builds the React frontend
3. Packages everything together
4. Creates the final runtime image

## Local Build

### Quick Start

Use the provided build script:

```bash
./docker-build.sh
```

This will build with default values:
- Version: 0.4.19.0
- Vendor: Readarr
- Branch: develop
- Image name: readarr
- Tag: latest

### Custom Build

You can customize the build with various options:

```bash
./docker-build.sh \
  --version "0.4.19.1" \
  --vendor "YourName" \
  --branch "feature-branch" \
  --image-name "my-readarr" \
  --tag "v1.0"
```

### Manual Docker Build

If you prefer to use Docker directly:

```bash
docker build \
  --build-arg VERSION="0.4.19.0" \
  --build-arg VENDOR="Readarr" \
  --build-arg BRANCH="develop" \
  --tag readarr:latest \
  .
```

## Running the Container

```bash
docker run -d \
  --name readarr \
  -p 8787:8787 \
  -v /path/to/config:/config \
  -v /path/to/books:/books \
  readarr:latest
```

## CI/CD Integration

The GitHub Actions workflow automatically builds and pushes Docker images to GitHub Container Registry (GHCR) on:
- Push to `develop` branch
- Push to `master` branch
- Pull requests to `develop` branch

### Image Tags

Images are tagged as:
- `ghcr.io/readarr/readarr:latest` - Latest build
- `ghcr.io/readarr/readarr:0.4.19.123` - Version-specific build

### Build Process

The CI workflow:
1. Sets version and build metadata
2. Runs tests (unit and integration)
3. Builds Docker image using multi-stage Dockerfile
4. Pushes to GHCR

## Build Arguments

| Argument | Description | Default |
|----------|-------------|---------|
| `VERSION` | Readarr version number | Required |
| `VENDOR` | Package author/vendor | Required |
| `BRANCH` | Git branch name | `develop` |
| `BUILD_CONFIGURATION` | .NET build configuration | `Release` |

## Multi-Stage Build Details

### Stage 1: Backend Builder
- Uses .NET 6.0 SDK Alpine image
- Builds all .NET projects
- Publishes for multiple platforms

### Stage 2: Frontend Builder
- Uses Node.js 20 Alpine image
- Installs dependencies with Yarn
- Builds React application

### Stage 3: Package Builder
- Combines backend and frontend builds
- Runs packaging scripts
- Creates final artifacts

### Stage 4: Runtime Image
- Uses Alpine 3.22
- Copies packaged application
- Sets up runtime environment

## Troubleshooting

### Build Issues

1. **Out of memory**: Increase Docker memory limit
2. **Network timeouts**: Check internet connection for package downloads
3. **Permission errors**: Ensure proper file permissions

### Runtime Issues

1. **Port conflicts**: Change the exposed port (8787)
2. **Volume mounts**: Ensure proper paths and permissions
3. **Configuration**: Check `/config` volume mount

## Development

For development builds, you can modify the Dockerfile to:
- Use development configurations
- Include debugging tools
- Enable hot reloading

Example development build:

```bash
docker build \
  --build-arg BUILD_CONFIGURATION=Debug \
  --build-arg BRANCH="dev" \
  --tag readarr:dev \
  .
``` 